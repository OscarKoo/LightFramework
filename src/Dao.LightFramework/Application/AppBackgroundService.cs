using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.Traces;
using Microsoft.Extensions.Hosting;

namespace Dao.LightFramework.Application;

public abstract class AppBackgroundService<TService> : BackgroundService
{
    protected readonly IServiceProvider serviceProvider;
    protected readonly SeriLogger<TService> logger;

    protected AppBackgroundService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        this.logger = new SeriLogger<TService>();
    }

    #region StartAsync

    public sealed override async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await OnStartAsync(cancellationToken);
            await base.StartAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            this.logger.Error(ex);
            throw;
        }
    }

    protected virtual Task OnStartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    #endregion

    #region ExecuteAsync

    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            TraceContext.Info.Renew().ModuleName = typeof(TService).Name;
            TraceContext.SpanId.Degrade(true);
            await OnExecuteAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            this.logger.Error(ex);
        }
    }

    protected virtual Task OnExecuteAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    #endregion

    #region StopAsync

    public sealed override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await OnStopAsync(cancellationToken);
            await base.StopAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            this.logger.Error(ex);
            throw;
        }
    }

    protected virtual Task OnStopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    #endregion

    #region Dispose

    public sealed override void Dispose()
    {
        try
        {
            OnDispose();
            base.Dispose();
        }
        catch (Exception ex)
        {
            this.logger.Error(ex);
        }
    }

    protected virtual void OnDispose() { }

    #endregion
}