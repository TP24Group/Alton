namespace TP24.Alton;

using System.Collections.Generic;

public class AltonOptions
{
    public AltonOptions()
    {
        this.BaseRoute = "/queue-management";
        this.QueuesToManage = new Dictionary<string, AltonQueueComponent>();
    }

    /// <summary>
    /// The base route to base Alton endpoints off, defaults to /api/queue
    /// </summary>
    public string BaseRoute { get; set; }

    /// <summary>
    /// The queues Alton should know about and be able to manage
    /// </summary>
    public Dictionary<string, AltonQueueComponent> QueuesToManage { get; set; }
}
