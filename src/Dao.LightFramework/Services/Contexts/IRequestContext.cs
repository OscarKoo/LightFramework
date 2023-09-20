namespace Dao.LightFramework.Services.Contexts;

public interface IRequestContext
{
    string Domain { get; set; }
    string Site { get; set; }
    string UserId { get; set; }
    string User { get; set; }
    string Language { get; set; }
    string Token { get; set; }

    void Reinitialize();
    void FillEntity<T>(T entity);
}