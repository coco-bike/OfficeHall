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

#region 配置 Serilog 
builder.Host.UseSerilog((context, logger) =>
{
    // 最小的日志输出级别,Serilog 人为写日志最小级别
    logger.MinimumLevel.Verbose();

    // 日志调用类命名空间如果以 Microsoft 开头，覆盖日志输出最小级别为 Information
    logger.MinimumLevel.Override("Microsoft", LogEventLevel.Information);
    logger.Enrich.FromLogContext();

    // 配置日志输出到控制台
    logger.WriteTo.Console();

    // 配置日志输出到文件，文件输出到当前项目的 logs 目录下，日记的生成周期为每天W
    logger.WriteTo.File(@".\Logs\Log.txt",     // 日志文件名
                       outputTemplate:                    // 设置输出格式，显示详细异常信息
                       @"{Timestamp:yyyy-MM-dd HH:mm:ss.fff }[{Level:u3}] {Message:lj}{NewLine}{Exception}",
                       rollingInterval: RollingInterval.Day,   // 日志按日保存
                       rollOnFileSizeLimit: true,              // 限制单个文件的最大长度
                       encoding: Encoding.UTF8);
});
#endregion

#region 配置FreeSql
IFreeSql DBFsql = new FreeSql.FreeSqlBuilder()
                                  .UseConnectionString(FreeSql.DataType.MySql, "")
                                  .UseAutoSyncStructure(true)
                                  .Build();

builder.Services.AddSingleton(DBFsql);
#endregion


#region 配置微信模块
var configuration = new ConfigurationManager();
builder.Services.AddSenparcGlobalServices(configuration)//Senparc.CO2NET 全局注册
                    .AddSenparcWeixinServices(configuration);//Senparc.Weixin 注册
#endregion
//配置信息注入
builder.Services.AddOptions().Configure<SenparcSetting>(configuration.GetSection("SenparcSetting"))
                            .Configure<SenparcWeixinSetting>(configuration.GetSection("SenparcWeixinSetting"));

builder.Services.AddControllers();
var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

#region  配置微信模块
var senparcSetting = app.Services.GetService<IOptions<SenparcSetting>>();
var senparcWeixinSetting = app.Services.GetService<IOptions<SenparcWeixinSetting>>();

// 启动 CO2NET 全局注册，必须！
IRegisterService register = RegisterService.Start(senparcSetting!.Value)
                                            .UseSenparcGlobal(false, null);
//开始注册微信信息，必须！
register.UseSenparcWeixin(senparcWeixinSetting!.Value, senparcSetting.Value);
#endregion

app.Run();
