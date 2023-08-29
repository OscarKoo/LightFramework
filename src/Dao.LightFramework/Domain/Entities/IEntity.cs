using Dao.LightFramework.Common.Attributes;

namespace Dao.LightFramework.Domain.Entities;

public interface IId
{
    string Id { get; set; }
}

public interface IRowVersion
{
    byte[] RowVersion { get; set; }
}

public interface IMutable
{
    [SwaggerIgnore]
    string CreateUser { get; set; }
    [SwaggerIgnore]
    DateTime? CreateTime { get; set; }
    [SwaggerIgnore]
    string UpdateUser { get; set; }
    [SwaggerIgnore]
    DateTime? UpdateTime { get; set; }
}

public interface IDomainSite
{
    [SwaggerIgnore]
    string Domain { get; set; }
    [SwaggerIgnore]
    string Site { get; set; }
}

public interface IDeleted
{
    [SwaggerIgnore]
    bool IsDeleted { get; set; }
}

public interface ILocked
{
    [SwaggerIgnore]
    string LockedUser { get; set; }
    [SwaggerIgnore]
    DateTime? LockedTime { get; set; }
}