using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace UniTracks.Services.Dispatching;

public interface IDispatcher
{
    void CreateTimer(TimeSpan interval);
    public void AddEventHandler(EventHandler eventHandler);
    public void RemoveEventHandler(EventHandler eventHandler);
    public void AddEventHandlerInMainThread(EventHandler eventHandler);
    public void RemoveAllEventHandlers();
    public void StopTimer();
    public void StartTimer();
}
