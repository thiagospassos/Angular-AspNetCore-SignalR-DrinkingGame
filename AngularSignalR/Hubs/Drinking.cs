using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace AngularSignalR.Hubs
{
    public class DrinkingHub : Hub
    {
        private static List<DrinkingGroup> _groups = new List<DrinkingGroup>();

        public override Task OnDisconnectedAsync(Exception exception)
        {
            if (_groups.Any(g => g.Owner == Context.ConnectionId))
            {
                var gs = _groups.Where(g => g.Owner == Context.ConnectionId).ToList();
                for (int i = 0; i < gs.Count; i++)
                {
                    BroadcastGroup(gs[i], true);
                    _groups.Remove(gs[i]);
                }
            }
            return base.OnDisconnectedAsync(exception);
        }

        public void CreateOrJoin(string key, string email)
        {
            var group = _groups.FirstOrDefault(g => g.Key == key);
            if (group == null)
            {
                group = new DrinkingGroup { Key = key, Owner = Context.ConnectionId };
                _groups.Add(group);
            }

            if (group.HasFinished || group.HasStarted)
            {
                throw new Exception("You cannot join a group which has started or finished");
            }

            group.Glasses.Add(new Glass { ConnectionId = Context.ConnectionId, Email = email });

            BroadcastGroup(group);
        }

        public void Start()
        {
            var group = _groups.FirstOrDefault(g => g.Owner == Context.ConnectionId && !g.HasFinished && !g.HasStarted);
            if (group != null)
            {
                group.HasStarted = true;
                BroadcastGroup(group);
            }
        }

        public void Drink()
        {
            var group = _groups.FirstOrDefault(g => !g.HasFinished && g.HasStarted && g.Glasses.Any(gl => gl.ConnectionId == Context.ConnectionId));
            if (group != null)
            {
                var glass = group.Glasses.First(g => g.ConnectionId == Context.ConnectionId);
                glass.Value--;
                if (glass.Value <= 0)
                {
                    group.HasFinished = true;
                    group.WinnerConnectionId = Context.ConnectionId;
                    group.WinnerEmail = glass.Email;
                }
                BroadcastGroup(group);
                if (group.HasFinished)
                {
                    _groups.Remove(group);
                }
            }
        }

        private void BroadcastGroup(DrinkingGroup group, bool removing = false)
        {
            var clients = group.Glasses.Select(g => g.ConnectionId).ToList();
            Clients.Clients(clients).SendAsync("Group", removing ? null : group);
        }

    }

    public class DrinkingGroup
    {
        public bool HasStarted { get; set; }
        public bool HasFinished { get; set; }
        public string Key { get; set; }
        public string WinnerConnectionId { get; set; }
        public string WinnerEmail { get; set; }
        //ConnectionId
        public string Owner { get; set; }
        public List<Glass> Glasses { get; set; } = new List<Glass>();
    }

    public class Glass
    {
        public bool HasLeft { get; set; }
        public string ConnectionId { get; set; }
        public string Email { get; set; }
        public int Value { get; set; } = 100;
    }
}