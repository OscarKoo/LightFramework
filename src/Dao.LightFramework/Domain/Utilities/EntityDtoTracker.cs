using Dao.LightFramework.Common.Utilities;
using Dao.LightFramework.Domain.Entities;

namespace Dao.LightFramework.Domain.Utilities;

public class EntityDtoTracker
{
    readonly Dictionary<object, HashSet<object>> entityDtos = new();

    public void Add(object entity, object dto)
    {
        var dtos = this.entityDtos.GetOrAdd(entity, k => new HashSet<object>());
        dtos.Add(dto);
    }

    public void MapToDtos()
    {
        foreach (var kv in this.entityDtos)
        {
            var entity = kv.Key as Entity;
            if (entity != null)
                entity.IgnoreRowVersionCheck = true;

            var type = kv.Key.GetType();
            foreach (var dto in kv.Value)
            {
                entity.Adapt(dto, type, dto.GetType(), false);
            }

            if (entity != null)
                entity.IgnoreRowVersionCheck = false;
        }
    }
}