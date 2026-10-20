using OCRS.Data; using OCRS.Models; using OCRS.Models.Enums;
namespace OCRS.Services;
public record CrmSyncResult(int Success,int Failed,int Skipped);
public interface ICrmSyncService { Task<CrmSyncResult> ImportSampleAsync(); }
public class CrmSyncService(ApplicationDbContext db):ICrmSyncService { public async Task<CrmSyncResult> ImportSampleAsync(){ var ids=new[]{"CRM-1001","CRM-1002","CRM-1003"}; var ok=0; foreach(var id in ids){ if(db.SyncLogs.Any(x=>x.ExternalId==id&&x.Status==SyncStatus.Success)){db.SyncLogs.Add(new SyncLog{ExternalId=id,Status=SyncStatus.Skipped,Message="Bản ghi đã được đồng bộ"});continue;} db.SyncLogs.Add(new SyncLog{ExternalId=id,Status=SyncStatus.Success,Message="Đã xác thực dữ liệu mẫu (mock adapter)"});ok++;} await db.SaveChangesAsync(); return new(ok,0,ids.Length-ok); } }
