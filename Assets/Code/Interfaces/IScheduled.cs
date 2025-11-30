public interface IScheduled {
    public void InitStep();
    public void RunStep();
    public void CleanupStep();
}