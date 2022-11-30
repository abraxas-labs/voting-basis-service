// (c) Copyright 2022 by Abraxas Informatik AG
// For license information see LICENSE file

using System;
using Abraxas.Voting.Basis.Events.V1;
using Abraxas.Voting.Basis.Events.V1.Data;
using AutoMapper;
using FluentValidation;
using Google.Protobuf;
using Voting.Basis.Core.Exceptions;
using Voting.Basis.Core.Utils;
using Voting.Basis.Data.Models;
using Voting.Lib.Common;
using Voting.Lib.Eventing.Domain;
using Voting.Lib.Iam.Exceptions;

namespace Voting.Basis.Core.Domain.Aggregate;

/// <summary>
/// Terminology is explained in <see cref="CountingCircle"/>.
/// </summary>
public sealed class CountingCircleAggregate : BaseDeletableAggregate
{
    private readonly IMapper _mapper;
    private readonly EventInfoProvider _eventInfoProvider;
    private readonly IValidator<CountingCirclesMerger> _ccMergerValidator;
    private readonly IClock _clock;

    public CountingCircleAggregate(
        IMapper mapper,
        EventInfoProvider eventInfoProvider,
        IValidator<CountingCirclesMerger> ccMergerValidator,
        IClock clock)
    {
        Name = string.Empty;
        Bfs = string.Empty;
        Code = string.Empty;
        NameForProtocol = string.Empty;
        ResponsibleAuthority = new Authority();
        ContactPersonDuringEvent = new ContactPerson();
        _mapper = mapper;
        _eventInfoProvider = eventInfoProvider;
        _ccMergerValidator = ccMergerValidator;
        _clock = clock;
    }

    public override string AggregateName => "voting-countingCircles";

    public string Name { get; private set; }

    public string Bfs { get; private set; }

    public string Code { get; private set; }

    public int SortNumber { get; private set; }

    public string NameForProtocol { get; private set; }

    public Authority ResponsibleAuthority { get; private set; }

    public ContactPerson ContactPersonDuringEvent { get; private set; }

    public bool ContactPersonSameDuringEventAsAfter { get; private set; }

    public ContactPerson? ContactPersonAfterEvent { get; private set; }

    public CountingCircleState State { get; private set; }

    public CountingCirclesMerger? MergerOrigin { get; private set; }

    public bool MergeActivationOverdue => MergerOrigin is { Merged: false } && MergerOrigin.ActiveFrom < _clock.UtcNow;

    public void CreateFrom(CountingCircle countingCircle)
    {
        if (countingCircle.Id == default)
        {
            countingCircle.Id = Guid.NewGuid();
        }

        var ev = new CountingCircleCreated
        {
            CountingCircle = _mapper.Map<CountingCircleEventData>(countingCircle),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void ScheduleMergeFrom(CountingCirclesMerger merger)
    {
        if (merger.Id == default)
        {
            merger.Id = Guid.NewGuid();
        }

        if (merger.NewCountingCircle.Id == default)
        {
            merger.NewCountingCircle.Id = Guid.NewGuid();
        }

        _ccMergerValidator.ValidateAndThrow(merger);
        ValidateMergeNotActivated();

        merger.ActiveFrom = merger.ActiveFrom.Date;
        ValidateDateTodayOrInFuture(merger.ActiveFrom, nameof(merger.ActiveFrom));

        var ev = new CountingCirclesMergerScheduled
        {
            Merger = _mapper.Map<CountingCirclesMergerEventData>(merger),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void UpdateScheduledMergerFrom(CountingCirclesMerger merger)
    {
        if (MergerOrigin == null)
        {
            throw new ValidationException("No merger set, cannot update");
        }

        merger.Id = MergerOrigin.Id;
        merger.NewCountingCircle.Id = Id;

        _ccMergerValidator.ValidateAndThrow(merger);
        ValidateMergeNotActivated();

        merger.ActiveFrom = merger.ActiveFrom.Date;
        ValidateDateTodayOrInFuture(merger.ActiveFrom, nameof(merger.ActiveFrom));

        var ev = new CountingCirclesMergerScheduleUpdated
        {
            Merger = _mapper.Map<CountingCirclesMergerEventData>(merger),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void CancelMerger()
    {
        if (MergerOrigin == null)
        {
            throw new ValidationException("No merger set, cannot delete");
        }

        ValidateMergeNotActivated();

        var ev = new CountingCirclesMergerScheduleDeleted
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            MergerId = MergerOrigin.Id.ToString(),
            NewCountingCircleId = Id.ToString(),
        };
        RaiseEvent(ev);
    }

    public void UpdateFrom(CountingCircle countingCircle, bool isAdmin)
    {
        EnsureNotDeletedOrMergedOrInactive();

        if (!isAdmin)
        {
            if (!ResponsibleAuthority.SecureConnectId.Equals(
                countingCircle.ResponsibleAuthority?.SecureConnectId, StringComparison.Ordinal))
            {
                throw new ForbiddenException(
                    "only users of the responsible authority or admins can update this entity");
            }

            if (!Bfs.Equals(countingCircle.Bfs, StringComparison.Ordinal))
            {
                throw new ForbiddenException("only admins are allowed to update the bfs");
            }

            if (!Name.Equals(countingCircle.Name, StringComparison.Ordinal))
            {
                throw new ForbiddenException("only admins are allowed to update the name");
            }

            if (!Code.Equals(countingCircle.Code, StringComparison.Ordinal))
            {
                throw new ForbiddenException("only admins are allowed to update the code");
            }

            if (SortNumber != countingCircle.SortNumber)
            {
                throw new ForbiddenException("only admins are allowed to update the sort number");
            }

            if (!NameForProtocol.Equals(countingCircle.NameForProtocol, StringComparison.Ordinal))
            {
                throw new ForbiddenException("only admins are allowed to update the name for protocol");
            }
        }

        var ev = new CountingCircleUpdated
        {
            CountingCircle = _mapper.Map<CountingCircleEventData>(countingCircle),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    public void Delete()
    {
        EnsureNotDeletedOrMergedOrInactive();
        var ev = new CountingCircleDeleted
        {
            CountingCircleId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    internal bool TryActivateMerge()
    {
        if (MergerOrigin?.Merged != false)
        {
            return false;
        }

        if (State != CountingCircleState.Inactive)
        {
            throw new ValidationException($"Counting Circle {Id} has State {State}. Activate is only on inactive counting circles allowed");
        }

        var ccMerger = _mapper.Map<CountingCirclesMergerEventData>(MergerOrigin);
        ccMerger.NewCountingCircle = _mapper.Map<CountingCircleEventData>(this);
        var ev = new CountingCirclesMergerActivated
        {
            EventInfo = _eventInfoProvider.NewEventInfo(),
            Merger = ccMerger,
        };

        RaiseEvent(ev);
        return true;
    }

    internal void SetMerged()
    {
        EnsureNotDeletedOrMergedOrInactive();
        var ev = new CountingCircleMerged
        {
            CountingCircleId = Id.ToString(),
            EventInfo = _eventInfoProvider.NewEventInfo(),
        };

        RaiseEvent(ev);
    }

    protected override void Apply(IMessage eventData)
    {
        switch (eventData)
        {
            case CountingCircleCreated e:
                State = CountingCircleState.Active;
                Apply(e);
                break;
            case CountingCirclesMergerScheduled e:
                State = CountingCircleState.Inactive;
                Apply(e);
                break;
            case CountingCirclesMergerScheduleUpdated e:
                Apply(e);
                break;
            case CountingCircleUpdated e:
                Apply(e);
                break;
            case CountingCircleDeleted _:
                State = CountingCircleState.Deleted;
                Deleted = true;
                break;
            case CountingCircleMerged _:
                State = CountingCircleState.Merged;
                Deleted = true;
                break;
            case CountingCirclesMergerScheduleDeleted _:
                State = CountingCircleState.Deleted;
                MergerOrigin = null;
                break;
            case CountingCirclesMergerActivated _:
                State = CountingCircleState.Active;
                MergerOrigin!.Merged = true;
                break;
            default: throw new EventNotAppliedException(eventData?.GetType());
        }
    }

    private void Apply(CountingCircleCreated ev)
    {
        _mapper.Map(ev.CountingCircle, this);
    }

    private void Apply(CountingCirclesMergerScheduled ev)
    {
        _mapper.Map(ev.Merger.NewCountingCircle, this);
        MergerOrigin = _mapper.Map<CountingCirclesMerger>(ev.Merger);
    }

    private void Apply(CountingCirclesMergerScheduleUpdated ev)
    {
        _mapper.Map(ev.Merger.NewCountingCircle, this);
        MergerOrigin = _mapper.Map<CountingCirclesMerger>(ev.Merger);
    }

    private void Apply(CountingCircleUpdated ev)
    {
        _mapper.Map(ev.CountingCircle, this);
    }

    private void EnsureNotDeletedOrMergedOrInactive()
    {
        if (State is CountingCircleState.Inactive or CountingCircleState.Deleted or CountingCircleState.Merged)
        {
            throw new ValidationException($"Counting Circle {Id} has State {State}. Modifications not allowed.");
        }
    }

    private void ValidateDateTodayOrInFuture(DateTime date, string name)
    {
        if (date.Date < _clock.UtcNow.Date)
        {
            throw new ValidationException($"{name} cannot be in the past");
        }
    }

    private void ValidateMergeNotActivated()
    {
        if (MergerOrigin is { Merged: true } || (MergerOrigin != null && MergerOrigin.ActiveFrom < _clock.UtcNow))
        {
            throw new CountingCircleMergerAlreadyActiveException();
        }
    }
}
