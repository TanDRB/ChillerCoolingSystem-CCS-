using System.Threading.Tasks;
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
            // Gửi ngay snapshot hiện có, không chờ lần đổi giá trị kế tiếp.
            var snapshot = _store.Snapshot();

            if (snapshot.Count > 0)
            {
                await Clients.Caller.SendAsync("MachineUpdate", snapshot);
            }

            await base.OnConnectedAsync();
        }
    }
}
