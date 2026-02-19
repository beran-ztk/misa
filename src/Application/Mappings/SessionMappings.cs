using Misa.Contract.Features.Entities.Extensions.Items.Features.Session;
using Misa.Domain.Items.Components.Activities.Sessions;

namespace Misa.Application.Mappings;

public static class SessionMappings
{
    public static SessionEfficiencyType MapToDomain(this EfficiencyContract contract) =>
        contract switch
        {
            EfficiencyContract.None            => SessionEfficiencyType.None,
            EfficiencyContract.LowOutput       => SessionEfficiencyType.LowOutput,
            EfficiencyContract.SteadyOutput    => SessionEfficiencyType.SteadyOutput,
            EfficiencyContract.HighOutput      => SessionEfficiencyType.HighOutput,
            EfficiencyContract.PeakPerformance => SessionEfficiencyType.PeakPerformance,
            _ => throw new ArgumentOutOfRangeException(nameof(contract), contract, null)
        };

    public static EfficiencyContract MapToDto(this SessionEfficiencyType domain) =>
        domain switch
        {
            SessionEfficiencyType.None            => EfficiencyContract.None,
            SessionEfficiencyType.LowOutput       => EfficiencyContract.LowOutput,
            SessionEfficiencyType.SteadyOutput    => EfficiencyContract.SteadyOutput,
            SessionEfficiencyType.HighOutput      => EfficiencyContract.HighOutput,
            SessionEfficiencyType.PeakPerformance => EfficiencyContract.PeakPerformance,
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null)
        };

    public static SessionConcentrationType MapToDomain(this ConcentrationContract contract) =>
        contract switch
        {
            ConcentrationContract.None              => SessionConcentrationType.None,
            ConcentrationContract.Distracted        => SessionConcentrationType.Distracted,
            ConcentrationContract.UnfocusedButCalm  => SessionConcentrationType.UnfocusedButCalm,
            ConcentrationContract.Focused           => SessionConcentrationType.Focused,
            ConcentrationContract.DeepFocus         => SessionConcentrationType.DeepFocus,
            ConcentrationContract.Hyperfocus        => SessionConcentrationType.Hyperfocus,
            _ => throw new ArgumentOutOfRangeException(nameof(contract), contract, null)
        };

    public static ConcentrationContract MapToDto(this SessionConcentrationType domain) =>
        domain switch
        {
            SessionConcentrationType.None              => ConcentrationContract.None,
            SessionConcentrationType.Distracted        => ConcentrationContract.Distracted,
            SessionConcentrationType.UnfocusedButCalm  => ConcentrationContract.UnfocusedButCalm,
            SessionConcentrationType.Focused           => ConcentrationContract.Focused,
            SessionConcentrationType.DeepFocus         => ConcentrationContract.DeepFocus,
            SessionConcentrationType.Hyperfocus        => ConcentrationContract.Hyperfocus,
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, null)
        };
}
