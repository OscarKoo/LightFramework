using MassTransit;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Dao.LightFramework.EntityFrameworkCore.EntityConfigurations;

public class IdGenerator : ValueGenerator
{
    protected override object NextValue(EntityEntry entry) => NewId.NextSequentialGuid().ToString();

    public override bool GeneratesTemporaryValues => false;
}