namespace EnterTheMatrix;

public class KeyListener
{
    private ConsoleKeyInfo _lastKeyPressed;
    private CancellationTokenSource _cts = new CancellationTokenSource();
        
    public EventHandler<ConsoleKeyInfo>? KeyPressed;


    public Task Start()
    {
        return Task.Run(Runner, _cts.Token);
    }

    public void Stop()
    {
        _cts.Cancel();
    }

    private void Runner()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                _lastKeyPressed = Console.ReadKey(true); // Read key without displaying
                KeyPressed?.Invoke(this, _lastKeyPressed);
                // Swallow any buffered keypresses
                while (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                }
            }

            Thread.Sleep(50); // Prevent excessive CPU usage
        }
    }

    public ConsoleKeyInfo GetLastKeyPressed()
    {
        return _lastKeyPressed;
    }
}