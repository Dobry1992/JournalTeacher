namespace Portal.Services.Interfaces
{
    public interface IShortNameParser
    {
        int[] GetNumericParts(string shortName);
    }
}
