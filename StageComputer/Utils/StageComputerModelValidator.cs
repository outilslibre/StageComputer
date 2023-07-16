using FluentValidation;
using OpenCloseDays;
using OpenCloseDays.Extensions;
using StageComputer.Pages;

namespace StageComputer.Utils
{
    public class StageComputerModelValidator : AbstractValidator<StageComputerModel>
    {
        private static DateTime[] GetOutOfRangeDates(
            IEnumerable<DateTime> dates, StageComputerModel x)
        {
            var startDate = x.StartDate <= x.EndDate ? x.StartDate : x.EndDate;
            var endDate = x.EndDate >= x.StartDate ? x.EndDate : x.StartDate;
            return dates.Where(d => d < startDate || d > endDate).ToArray();
        }
        private static string[] GetOutOfRangePublicHolidayNames(
                PublicHolidaysService publicHolidaysService,
                IEnumerable<string> publicHolidaysNames, 
                StageComputerModel x)
        {
            var startDate = x.StartDate <= x.EndDate ? x.StartDate : x.EndDate;
            var endDate = x.EndDate >= x.StartDate ? x.EndDate : x.StartDate;
            var publicHolidaysInPeriod = publicHolidaysService.GetPublicHolidaysForPeriod(x.Country, startDate, endDate).Select(p => p.Name).ToArray();
            return publicHolidaysNames.Where(n => !publicHolidaysInPeriod.Contains(n)).ToArray();
        }
        public StageComputerModelValidator(PublicHolidaysService publicHolidaysService)
        {
            RuleFor(x => x.ExcludedDateTimes).Must((x, dates) =>
            {
                return !GetOutOfRangeDates(dates, x).Any();
            })
                .WithMessage(x => string.Format("Les dates suivantes sont en dehors de la période de stage : {0}.",
                                    string.Join(",", GetOutOfRangeDates(x.ExcludedDateTimes, x).Select(d => d.ToShortDateString()))));
            RuleFor(x => x.WorkPublicHolidays).Must((x, names) =>
            {
                return !GetOutOfRangePublicHolidayNames(publicHolidaysService, names, x).Any();
            })
                .WithMessage(x => string.Format("Les jours fériés suivants sont en dehors de la période de stage : {0}.",
                                    string.Join(",", GetOutOfRangePublicHolidayNames(publicHolidaysService, x.WorkPublicHolidays, x))));

            RuleFor(x => x.PayRate).NotEmpty().Must(v => v.CanParseAsFloat());
            RuleFor(x => x.StartDate).LessThanOrEqualTo(x => x.EndDate)
                .WithMessage("La date de début doit être inférieure à la date de fin du stage.");
            RuleFor(x => x.Country).NotEmpty().Must(v => "fr" != v).When(x => x.StageCountry == StageLegislation.OUTSIDE_FRANCE)
                .WithMessage("Le pays ne doit pas être France pour un stage à l'étranger.");
            RuleFor(x => x.Country).NotEmpty().Must(v => "fr" == v).When(x => x.StageCountry == StageLegislation.IN_FRANCE)
                .WithMessage("Le pays doit être France pour un stage en France.");
		}
	}
}
