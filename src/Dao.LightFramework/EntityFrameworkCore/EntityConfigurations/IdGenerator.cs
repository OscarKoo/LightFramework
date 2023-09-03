using Dao.LightFramework.Common.Utilities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace Dao.LightFramework.EntityFrameworkCore.EntityConfigurations;

public class IdGenerator : ValueGenerator
{
    protected override object NextValue(EntityEntry entry) => NewGuid.NextSequential();

    public override bool GeneratesTemporaryValues => false;
}