using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;

namespace HwPulse.Sensors;

public sealed class TelemetryJournal(string directory)
{
    private const long HardwareLimit = 16 * 1024 * 1024;
    private const long ProcessLimit = 32 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
    private DateTimeOffset nextProcessSample;
    private bool failed;

    public void Publish(Snapshot snapshot, DateTimeOffset acquiredAt)
    {
        try
        {
            Append("telemetry", HardwareLimit, new { schema_version = 1, captured_at = acquiredAt, snapshot });
            if (acquiredAt >= nextProcessSample)
            {
                nextProcessSample = acquiredAt.AddSeconds(5);
                Append("processes", ProcessLimit, new { schema_version = 1, captured_at = DateTimeOffset.UtcNow, processes = ProcessTelemetry.Read() });
            }
            failed = false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or System.ComponentModel.Win32Exception)
        {
            if (!failed) Trace.TraceWarning("HW Pulse: {0}", ex.Message);
            failed = true;
        }
    }

    internal void Append<T>(string name, long limit, T value)
    {
        var file = new FileInfo(Path.Combine(directory, name + ".jsonl"));
        var data = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        if (file.Exists && file.Length + data.Length + 1 > limit)
        {
            file.MoveTo(Path.Combine(directory, name + ".previous.jsonl"), overwrite: true);
            file = new FileInfo(Path.Combine(directory, name + ".jsonl"));
        }
        using var stream = file.Exists
            ? file.Open(FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete)
            : file.Create(FileMode.CreateNew, FileSystemRights.AppendData, FileShare.ReadWrite | FileShare.Delete,
                4096, FileOptions.None, JournalSecurity());
        stream.Write(data);
        stream.WriteByte((byte)'\n');
    }

    private static FileSecurity JournalSecurity()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var owner = identity.User ?? throw new UnauthorizedAccessException();
        var security = new FileSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.SetOwner(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null));
        security.AddAccessRule(new FileSystemAccessRule(owner, FileSystemRights.Read, AccessControlType.Allow));
        foreach (var sid in new[] { new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                     new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null) })
        {
            security.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl, AccessControlType.Allow));
        }
        return security;
    }
}
