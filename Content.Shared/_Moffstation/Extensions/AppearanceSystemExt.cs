namespace Content.Shared._Moffstation.Extensions;

public static partial class AppearanceSystemExt
{
    extension(SharedAppearanceSystem appearanceSystem)
    {
        /// <see cref="SharedAppearanceSystem.SetData"/> if <paramref name="value"/> is not <c>null</c>, otherwise
        /// <see cref="SharedAppearanceSystem.RemoveData"/>.
        public void SetOrRemoveData(Entity<AppearanceComponent?> entity, Enum key, object? value)
        {
            if (value != null)
            {
                appearanceSystem.SetData(entity, key, value, entity);
            }
            else
            {
                appearanceSystem.RemoveData(entity, key, entity);
            }
        }
    }
}
