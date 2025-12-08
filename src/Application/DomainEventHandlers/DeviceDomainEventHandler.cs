using NextAdmin.Application.Interfaces;
using NextAdmin.Application.Services;
using NextAdmin.Core.Domain.Events;
using MediatR;
using ModbusGateway;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NextAdmin.Application.DomainEventHandlers
{
    public class DeviceDomainEventHandler : INotificationHandler<DeviceDomainEvent>
    {
        private readonly GatewayServer _gatewayServer;
        private readonly IGatewayAppService _gatewayAppService;
        private readonly IProjectAppService _projectAppService;

        public DeviceDomainEventHandler(
            GatewayServer gatewayServer,
            IGatewayAppService gatewayAppService,
            IProjectAppService projectAppService
            )
        {
            _gatewayServer=gatewayServer;
            _gatewayAppService = gatewayAppService;
            _projectAppService = projectAppService;
        }

        public async Task Handle(DeviceDomainEvent notification, CancellationToken cancellationToken)
        {
            // First sync Device instances for all gateway clients
            foreach (var client in _gatewayServer.GetAllClients())
            {
                if (client.GatewayId == notification.Entity.GatewayId.ToString())
                    client.UpdateDevice(notification.Entity);
            }

            await _projectAppService.UpdateAllProjectCountsAsync();
            await _gatewayAppService.UpdateDeviceCount();

        }
    }
}
