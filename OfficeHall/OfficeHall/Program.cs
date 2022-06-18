using Microsoft.Extensions.Options;
using Senparc.CO2NET;
using Senparc.CO2NET.RegisterServices;
using Senparc.Weixin;
using Senparc.Weixin.Entities;
using Senparc.Weixin.RegisterServices;
using Serilog;
using Serilog.Events;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

#region ���� Serilog 
builder.Host.UseSerilog((context, logger) =>
{
    // ��С����־�������,Serilog ��Ϊд��־��С����
    logger.MinimumLevel.Verbose();

    // ��־�����������ռ������ Microsoft ��ͷ��������־�����С����Ϊ Information
    logger.MinimumLevel.Override("Microsoft", LogEventLevel.Information);
    logger.Enrich.FromLogContext();

    // ������־���������̨
    logger.WriteTo.Console();

    // ������־������ļ����ļ��������ǰ��Ŀ�� logs Ŀ¼�£��ռǵ���������Ϊÿ��W
    logger.WriteTo.File(@".\Logs\Log.txt",     // ��־�ļ���
                       outputTemplate:                    // ���������ʽ����ʾ��ϸ�쳣��Ϣ
                       @"{Timestamp:yyyy-MM-dd HH:mm:ss.fff }[{Level:u3}] {Message:lj}{NewLine}{Exception}",
                       rollingInterval: RollingInterval.Day,   // ��־���ձ���
                       rollOnFileSizeLimit: true,              // ���Ƶ����ļ�����󳤶�
                       encoding: Encoding.UTF8);
});
#endregion

#region ����FreeSql
IFreeSql DBFsql = new FreeSql.FreeSqlBuilder()
                                  .UseConnectionString(FreeSql.DataType.MySql, "")
                                  .UseAutoSyncStructure(true)
                                  .Build();

builder.Services.AddSingleton(DBFsql);
#endregion


#region ����΢��ģ��
var configuration = new ConfigurationManager();
builder.Services.AddSenparcGlobalServices(configuration)//Senparc.CO2NET ȫ��ע��
                    .AddSenparcWeixinServices(configuration);//Senparc.Weixin ע��
#endregion
//������Ϣע��
builder.Services.AddOptions().Configure<SenparcSetting>(configuration.GetSection("SenparcSetting"))
                            .Configure<SenparcWeixinSetting>(configuration.GetSection("SenparcWeixinSetting"));

builder.Services.AddControllers();
var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

#region  ����΢��ģ��
var senparcSetting = app.Services.GetService<IOptions<SenparcSetting>>();
var senparcWeixinSetting = app.Services.GetService<IOptions<SenparcWeixinSetting>>();

// ���� CO2NET ȫ��ע�ᣬ���룡
IRegisterService register = RegisterService.Start(senparcSetting!.Value)
                                            .UseSenparcGlobal(false, null);
//��ʼע��΢����Ϣ�����룡
register.UseSenparcWeixin(senparcWeixinSetting!.Value, senparcSetting.Value);
#endregion

app.Run();
