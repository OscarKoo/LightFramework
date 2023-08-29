namespace Dao.LightFramework.Services.Contexts;

public interface IMultilingual
{
    string Get(ICollection<string> keys);
    string Get(ICollection<string> keys, params object[] args);
    string Get(string key, params object[] args);
    string GetByLocale(string locale = null);
    string GetAll();
}