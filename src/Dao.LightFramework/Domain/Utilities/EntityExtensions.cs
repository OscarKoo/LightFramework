using Dao.LightFramework.Common.Exceptions;
using Dao.LightFramework.Domain.Entities;
using Dao.LightFramework.Services.Contexts;

namespace Dao.LightFramework.Domain.Utilities;

public static class EntityExtensions
{
    public static bool IsDtoExpired(this IRowVersion entity, IRowVersion dto, IMultilingual lang = null)
    {
        if (entity?.RowVersion == null)
            return false;

        if (dto.RowVersion == null)
            return lang == null ? true : throw new DataHasChangedException(lang.Get("数据缺少RowVersion字段. ({0}: {1})", entity.GetType().Name, (entity as IId)?.Id));

        if (!entity.RowVersion.SequenceEqual(dto.RowVersion))
            return lang == null ? true : throw new DataHasChangedException(lang.Get("数据已被其他用户修改, 请重新获取. ({0}: {1})", entity.GetType().Name, (entity as IId)?.Id));

        return false;
    }

    public static bool IsNewerThan(this IRowVersion entity, IRowVersion dto)
    {
        if (entity?.RowVersion == null || entity.RowVersion.Length == 0)
            return false;

        if (dto?.RowVersion == null || dto.RowVersion.Length == 0)
            return true;

        for (var i = 0; i < entity.RowVersion.Length; i++)
        {
            var e = entity.RowVersion[i];
            var d = dto.RowVersion[i];
            if (e > d)
                return true;
        }

        return false;
    }
}