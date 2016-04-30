namespace Hub
{
    public interface IMultiSessionPlugin : ISingleSessionPlugin
    {
        IMultiSessionPlugin Clone();
    }
}
