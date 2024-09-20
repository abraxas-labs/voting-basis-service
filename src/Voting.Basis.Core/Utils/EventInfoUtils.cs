// (c) Copyright by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using System.Reflection;
using Google.Protobuf;
using Proto = Abraxas.Voting.Basis.Events.V1.Data;

namespace Voting.Basis.Core.Utils;

public static class EventInfoUtils
{
    public static PropertyInfo GetEventInfoPropertyInfo(IMessage message)
    {
        return message.GetType().GetProperty(nameof(Proto.EventInfo))
            ?? throw new ArgumentException("Event has no EventInfo field", nameof(message));
    }

    public static Proto.EventInfo GetEventInfo(IMessage message)
    {
        var eventInfoProp = GetEventInfoPropertyInfo(message);
        var eventInfoObj = eventInfoProp.GetValue(message);

        if (eventInfoObj is Proto.EventInfo eventInfo)
        {
            return eventInfo;
        }

        throw new ArgumentException("Could not retrieve event info value", nameof(eventInfo));
    }
}
