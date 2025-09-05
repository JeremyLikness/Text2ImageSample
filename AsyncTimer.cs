namespace Text2ImageSample
{
    public class AsyncTimer
    {
        const int limitMs = 100;
        const string Spinner = @"|/-\";
        const string Format = @"hh\:mm\:ss\.fff";

        private readonly TimeSpan limit = TimeSpan.FromMilliseconds(limitMs);
        private int _spindex = 0;
        private readonly long _init = DateTime.Now.Ticks;
        private long _taskStart = DateTime.Now.Ticks;
        private long _lastPing = DateTime.Now.Ticks;

        public void NewTask() => _taskStart = DateTime.Now.Ticks;

        public void Ping()
        {
            var now = DateTime.Now.Ticks;
            var elapsed = TimeSpan.FromTicks(now - _lastPing);
            if (elapsed < limit)
            {
                return;
            }
            _lastPing = now;
            _spindex = (++_spindex) % Spinner.Length;

            var totalElapsed = TimeSpan.FromTicks(now - _init).ToString(Format);
            var taskElapsed = TimeSpan.FromTicks(now - _taskStart).ToString(Format);

            Console.Write($"[Elapsed (total): {totalElapsed} {Spinner[_spindex]} Elapsed (task): {taskElapsed}");
            Console.SetCursorPosition(0, Console.GetCursorPosition().Top);
        }
    }
}
