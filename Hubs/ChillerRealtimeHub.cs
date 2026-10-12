using ChillerCoolingSystem_CCS_.Services;
using Microsoft.AspNetCore.SignalR;

namespace ChillerCoolingSystem_CCS_.Hubs
{
    public class ChillerRealtimeHub : Hub
    {
        private readonly MachineLatestValuesStore _store;

        public ChillerRealtimeHub(MachineLatestValuesStore store)
        {
            _store = store;
        }

        public override async Task OnConnectedAsync()
        {
            var snapshot = _store.Snapshot();
            if (snapshot.Count > 0)
            {
                await Clients.Caller.SendAsync("MachineUpdate", snapshot);
            }
            await base.OnConnectedAsync();
        }
    }
}
