using StageComputer.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PublicHoliday;
using SpreadCheetah;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using OpenCloseDays.Models;
using OpenCloseDays;
using OpenCloseDays.Extensions;
using FluentValidation;

namespace StageComputer.Pages
{
    public class StageComputerModel : PageModel
    {
        #region Bound poperties
        [BindProperty(Name = "s", SupportsGet = true)]
        [Display(Name = "Date début")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [BindProperty(Name = "e", SupportsGet = true)]
        [Display(Name = "Date fin")]
        public DateTime EndDate { get; set; } = DateTime.Now;

        [BindProperty(Name = "monh", SupportsGet = true)]
        [Display(Name = "Lundi")]
        public int MondayWorkHours { get; set; } = 7;
        [BindProperty(Name = "monm", SupportsGet = true)]
        [Display(Name = "Lundi")]
        public int MondayWorkMinutes { get; set; } = 0;

        [BindProperty(Name = "tueh", SupportsGet = true)]
        [Display(Name = "Mardi")]
        public int TuesdayWorkHours { get; set; } = 7;
        [BindProperty(Name = "tuem", SupportsGet = true)]
        [Display(Name = "Mardi")]
        public int TuesdayWorkMinutes { get; set; } = 0;

        [BindProperty(Name = "wedh", SupportsGet = true)]
        [Display(Name = "Mercredi")]
        public int WednesdayWorkHours { get; set; } = 7;
        [BindProperty(Name = "wedm", SupportsGet = true)]
        [Display(Name = "Mercredi")]
        public int WednesdayWorkMinutes { get; set; } = 0;

        [BindProperty(Name = "thuh", SupportsGet = true)]
        [Display(Name = "Jeudi")]
        public int ThursdayWorkHours { get; set; } = 7;
        [BindProperty(Name = "thum", SupportsGet = true)]
        [Display(Name = "Jeudi")]
        public int ThursdayWorkMinutes { get; set; } = 0;

        [BindProperty(Name = "frih", SupportsGet = true)]
        [Display(Name = "Vendredi")]
        public int FridayWorkHours { get; set; } = 7;
        [BindProperty(Name = "frim", SupportsGet = true)]
        [Display(Name = "Vendredi")]
        public int FridayWorkMinutes { get; set; } = 0;

        [BindProperty(Name = "sath", SupportsGet = true)]
        [Display(Name = "Samedi")]
        public int SaturdayWorkHours { get; set; } = 0;
        [BindProperty(Name = "satm", SupportsGet = true)]
        [Display(Name = "Samedi")]
        public int SaturdayWorkMinutes { get; set; } = 0;

        [BindProperty(Name = "sunh", SupportsGet = true)]
        [Display(Name = "Dimanche")]
        public int SundayWorkHours { get; set; } = 0;
        [BindProperty(Name = "sunm", SupportsGet = true)]
        [Display(Name = "Dimanche")]
        public int SundayWorkMinutes { get; set; } = 0;


        [BindProperty(Name = "excl", SupportsGet = true)]
        public string? ExcludedDates { get; set; }


        [BindProperty(Name = "r", SupportsGet = true)]
        [Display(Name = "Montant horaire")]
        public string? PayRate { get; set; } = "0";

        [BindProperty(Name = "c", SupportsGet = true)]
        [Display(Name = "Pays")]
        public string? Country { get; set; } = "fr";
        [BindProperty(Name = "stc", SupportsGet = true)]
        public string? StageCountry { get; set; } = StageLegislation.IN_FRANCE;
        [BindProperty(Name = "ent", SupportsGet = true)]
        public string? EnterpriseType { get; set; } = StageLegislation.ENTERPRISE_TYPE_PRIVATE;
        [BindProperty(Name = "tt", SupportsGet = true)]
        public string? TrainingType { get; set; } = StageLegislation.TRAINING_OTHER;
        [BindProperty(Name = "wph", SupportsGet = true)]
        [Display(Name = "Si vous travaillez certains jours fériés, veuillez sélectionner ces jours")]
        public IEnumerable<string>? WorkPublicHolidays { get; set; } = Array.Empty<string>();
        #endregion

        public IEnumerable<SelectListItem> Countries { get; }
        public IEnumerable<SelectListItem> HolidayNames { get; private set; }
        public WorkDaysComputationResult Result { get; set; }

        public DateTime? LongStageStartDate { get; set; }

        private readonly ILogger<StageComputerModel> _logger;
        private readonly OpenCloseDaysService _openCloseDaysService;
        private readonly PlafondSecuFetcher plafondSecuFetcher;
        private readonly ExplainationManager explainationManager;
        private readonly IValidator<StageComputerModel> validator;
        private readonly PublicHolidaysService _publicHolidaysService;

        public StageComputerModel(
            ILogger<StageComputerModel> logger,
            OpenCloseDaysService openCloseDaysService,
            PlafondSecuFetcher plafondSecuFetcher,
            ExplainationManager explainationManager,
            IValidator<StageComputerModel> validator,
            PublicHolidaysService publicHolidaysService)
        {
            _logger = logger;
            _openCloseDaysService = openCloseDaysService;
            this.plafondSecuFetcher = plafondSecuFetcher;
            this.explainationManager = explainationManager;
            this.validator = validator;
            _publicHolidaysService = publicHolidaysService;

            Countries = _publicHolidaysService.GetHandledCountries().Select(c => new SelectListItem(c.name, c.code));
        }

        public async Task OnGetAsync()
        {
            await ComputeDaysAsync();
        }
        public async Task OnPostAsync()
        {
            await ComputeDaysAsync();
        }
        public async Task<FileResult> OnGetExcelAsync()
        {
            await ComputeDaysAsync();

            var excelBytes = await StageExcel.GenerateExcel(Result, PayRate.ParseAsFloat());
            var fileName = $"Calcul durée et gratification de stage du {Result.StartDate:dd/MM/yyyy} au {Result.EndDate:dd/MM/yyyy}.xlsx";
            return File(excelBytes,
                StageExcel.XLSX_Mimetype,
                fileName);
        }

        private void EnsureStartBeforeEnd()
        {
            if (StartDate.Date > EndDate.Date)
            {
                var tmp = StartDate;
                StartDate = EndDate;
                EndDate = tmp;
            }
        }

        private async Task ComputeDaysAsync()
        {
            HolidayNames = _publicHolidaysService.GetPublicHolidaysForYear(Country, DateTime.Now.Year).Select(h => new SelectListItem($"{h.Value} ({h.Key.ToString("dd/MM/yyyy")})", h.Value));
			
            foreach (var error in validator.Validate(this).Errors)
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

            EnsureStartBeforeEnd();

            explainationManager.RegisterVariable("taux-horaire", (await plafondSecuFetcher.GetLegalStagePayRateAsync(StartDate)).ToString("0.00",CultureInfo.InvariantCulture));
            explainationManager.RegisterVariable("taux-horaire_année", StartDate.Year.ToString());
            explainationManager.RegisterVariable("duree-legale", StageLegislation.LongStage_Hours.ToString());
            explainationManager.RegisterVariable("duree-legale-plus", (StageLegislation.LongStage_Hours + 1).ToString());
            explainationManager.RegisterVariable("pourcent-phss", (StageLegislation.LEGAL_PAYRATE_PLAFOND_RATIO * 100).ToString("0.#"));

            Result = _openCloseDaysService.ComputeWorkDays(
                Country, StartDate, EndDate, new DaysWorkHours()
                {
                    MondayWorkHours = MondayWorkHours,
                    MondayWorkMinutes = MondayWorkMinutes,
                    TuesdayWorkHours = TuesdayWorkHours,
                    TuesdayWorkMinutes = TuesdayWorkMinutes,
                    WednesdayWorkHours = WednesdayWorkHours,
                    WednesdayWorkMinutes = WednesdayWorkMinutes,
                    ThursdayWorkHours = ThursdayWorkHours,
                    ThursdayWorkMinutes = ThursdayWorkMinutes,
                    FridayWorkHours = FridayWorkHours,
                    FridayWorkMinutes = FridayWorkMinutes,
                    SaturdayWorkHours = SaturdayWorkHours,
                    SaturdayWorkMinutes = SaturdayWorkMinutes,
                    SundayWorkHours = SundayWorkHours,
                    SundayWorkMinutes = SundayWorkMinutes,
                },
                WorkPublicHolidays.ToArray(), null, ExcludedDateTimes.ToArray());

            await StageLegislation.EnsureLegislationForPayRateAsync(
                plafondSecuFetcher, StartDate,
                PayRate.ParseAsFloat(), EnterpriseType, StageCountry, TrainingType,
                Result.TotalOpenHours, async (newRate, messsage) =>
                {
                    PayRate = newRate.ToString(CultureInfo.InvariantCulture);
                    ModelState.AddModelError(nameof(PayRate),
                        messsage);
                });

            if (!StageLegislation.IsInAllowedHours(Result.TotalOpenHours))
            {
                ModelState.AddModelError(nameof(Result),
                    StageLegislation.GetTooLongStageHoursMessage());
            }

            LongStageStartDate = StageLegislation.GetLongStageStartDate(Result);
        }

        public IEnumerable<DateTime> ExcludedDateTimes
            => ExcludedDates.ParseAsDatesList();
    }
}