// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Linq;
using System.Threading.Tasks;
using Abraxas.Voting.Basis.Events.V1.Data;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Voting.Basis.Core.Extensions;
using Voting.Basis.Data;
using Voting.Lib.Eventing.Testing.Mocks;
using Voting.Lib.Iam.Testing.AuthenticationScheme;

namespace Voting.Basis.Test;

public class TestEventPublisherAdapter
{
    private const string EventInfoFieldName = "EventInfo";

    private readonly TestEventPublisher _testEventPublisher;
    private readonly DataContext _dataContext;

    public TestEventPublisherAdapter(TestEventPublisher testEventPublisher, DataContext dataContext)
    {
        _testEventPublisher = testEventPublisher;
        _dataContext = dataContext;
    }

    public static EventInfo GetMockedEventInfo()
    {
        return new EventInfo
        {
            Timestamp = new Timestamp
            {
                Seconds = 1594980476,
            },
            Tenant = SecureConnectTestDefaults.MockedTenantDefault.ToEventInfoTenant(),
            User = SecureConnectTestDefaults.MockedUserDefault.ToEventInfoUser(),
        };
    }

    /// <summary>
    /// Publishes test events and sets a default value for EventInfo if not set.
    /// </summary>
    /// <param name="data">The events.</param>
    /// <typeparam name="TEvent">Type of the events.</typeparam>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task Publish<TEvent>(params TEvent[] data)
        where TEvent : IMessage<TEvent>
            => Publish(0, data);

    /// <summary>
    /// Publishes test events and sets a default value for EventInfo if not set.
    /// </summary>
    /// <param name="eventNumber">The event number of the first event.</param>
    /// <param name="data">The events.</param>
    /// <typeparam name="TEvent">Type of the events.</typeparam>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task Publish<TEvent>(long eventNumber, params TEvent[] data)
        where TEvent : IMessage<TEvent>
    {
        using var nQueryDetector = _dataContext.CreateNQueryDetectorSpan(data.Length);
        var propInfo = typeof(TEvent)
            .GetProperties()
            .FirstOrDefault(x => x.Name.Equals(EventInfoFieldName, StringComparison.OrdinalIgnoreCase));

        if (propInfo != null)
        {
            foreach (var item in data)
            {
                if (propInfo.GetValue(item) == null)
                {
                    propInfo.SetValue(item, GetMockedEventInfo());
                }
            }
        }

        await _testEventPublisher.Publish(eventNumber, data);
    }
}
