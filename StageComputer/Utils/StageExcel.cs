using OpenCloseDays.Models;
using SpreadCheetah;
using SpreadCheetah.Worksheets;
using System.Drawing;
using System.IO;

namespace StageComputer.Utils
{
    public class StageExcel
    {
        public const string XLSX_Mimetype = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const string Worksheet_Name = "Jours travaillés";
        private const string StageDate_ColumnTitle = "Jours";
        private const string StageWorkHours_ColumnTitle = "Heures";
        private const string StageWorkHoursSum_ColumnTitle = "Cumul Heures";
        private const string TotalWorkHours_RowTitle = "Total heures";
        private const string TotalWorkDays_RowTitle = "Total jours";
        private const string PayRate_RowTitle = "Taux horaire";
        private const string TotalPaidAmount_RowTitle = "Total gratification";
        private const string MonthName_ColumnTitle = "Mois";
        private const string MonthHours_ColumnTitle = "Heures";
        private const string MonthPaidAmount_ColumnTitle = "Gratification";
        private const string Excel_Title = "Calcul de la durée et de la gratification de stage";
        private const string Explaination1 = "Si le rythme hebdomadaire de votre présence dans l’organisme d’accueil varie au cours du stage, modifiez dans la colonne « Heures » votre temps de présence journalière effective.";
        private const string Explaination2 = "Le tableau calculera automatiquement votre temps de présence et le montant de votre gratification.";

        public static async Task<byte[]> GenerateExcel(
            WorkDaysComputationResult workDaysComputation,
            float payRate)
        {
            using (var outputStream = new MemoryStream())
            {
                using (var spreadsheet = await Spreadsheet.CreateNewAsync(outputStream))
                {

                    var worksheetOptions = new WorksheetOptions();
                    worksheetOptions.Column(1).Width = 25;
                    worksheetOptions.Column(2).Width = 20;
                    worksheetOptions.Column(3).Width = 20;
                    await spreadsheet.StartWorksheetAsync(Worksheet_Name, worksheetOptions);

                    var boldStyleId = spreadsheet.AddStyle(new SpreadCheetah.Styling.Style()
                    {
                        Font = new SpreadCheetah.Styling.Font() { Bold = true }
                    });
                    var moneyStyleId = spreadsheet.AddStyle(new SpreadCheetah.Styling.Style()
                    {
                        NumberFormat = "0.00€"
                    });
                    var dateStyleId = spreadsheet.AddStyle(new SpreadCheetah.Styling.Style()
                    {
                        NumberFormat = "DDDD DD MMM YYYY"
                    });
                    var dateGrayStyleId = spreadsheet.AddStyle(new SpreadCheetah.Styling.Style()
                    {
                        NumberFormat = "DDDD DD MMM YYYY",
                        Fill = new SpreadCheetah.Styling.Fill() { Color = Color.LightGray }
                    });
                    var grayStyleId = spreadsheet.AddStyle(new SpreadCheetah.Styling.Style()
                    {
                        Fill = new SpreadCheetah.Styling.Fill() { Color = Color.LightGray }
                    });

                    const int headersCount = 4;

                    await spreadsheet.AddRowAsync(new[] {
                        new Cell(Excel_Title, boldStyleId)
                    });
                    await spreadsheet.AddRowAsync(new[] {
                        new Cell(Explaination1)
                    });
                    await spreadsheet.AddRowAsync(new[] {
                        new Cell(Explaination2)
                    });

                    await spreadsheet.AddRowAsync(new[] {
                        new Cell(StageDate_ColumnTitle, boldStyleId),
                        new Cell(StageWorkHours_ColumnTitle, boldStyleId),
                        new Cell(StageWorkHoursSum_ColumnTitle, boldStyleId)
                    });
                    var rowNum = headersCount + 1;
                    var daysCount = workDaysComputation.DatesWorkHours.Count;
                    foreach (var dayHours in workDaysComputation.DatesWorkHours)
                    {
                        var isWorked = DayOfWeek.Saturday != dayHours.Date.DayOfWeek
                            && DayOfWeek.Sunday != dayHours.Date.DayOfWeek
                            && !workDaysComputation.PublicHolidayDays.Any(publicHoliday => publicHoliday.Date.Date == dayHours.Date.Date);
                        await spreadsheet.AddRowAsync(new[] {
                            new Cell(dayHours.Date, isWorked ? dateStyleId : dateGrayStyleId),
                            new Cell(dayHours.WorkedHours, isWorked ? null : grayStyleId),
                            new Cell(new Formula($"SUM(B{headersCount + 1}:B{rowNum})"), isWorked ? null : grayStyleId)
                        });
                        rowNum++;
                    }

                    var payRateCell = $"B{daysCount + headersCount + 1 + 1 + 1}";
                    var totalHoursCell = $"B{daysCount + headersCount + 1}";

                    await spreadsheet.AddRowAsync(new[] {
                        new Cell(TotalWorkHours_RowTitle, boldStyleId),
                        new Cell(new Formula($"SUM(B{headersCount + 1}:B{daysCount + headersCount})")) });
                    await spreadsheet.AddRowAsync(new[] {
                        new Cell(TotalWorkDays_RowTitle, boldStyleId),
                        new Cell(new Formula($"B{daysCount + headersCount + 1} / 7")) });
                    await spreadsheet.AddRowAsync(new[] {
                        new Cell(PayRate_RowTitle, boldStyleId),
                        new Cell( payRate, moneyStyleId ) });
                    await spreadsheet.AddRowAsync(new[] {
                        new Cell(TotalPaidAmount_RowTitle, boldStyleId),
                        new Cell(new Formula($"{totalHoursCell}*{payRateCell}"), moneyStyleId) });


                    await spreadsheet.AddRowAsync(new[] {
                        new Cell(MonthName_ColumnTitle, boldStyleId),
                        new Cell(MonthHours_ColumnTitle, boldStyleId),
                        new Cell(MonthPaidAmount_ColumnTitle, boldStyleId),
                    });

                    var dateRowNum = headersCount + 1;
                    foreach (var monthWorkHours in workDaysComputation.MonthsWorkHours)
                    {
                        var hoursSumForMonthFormula = string.Join("+", Enumerable.Range(dateRowNum, monthWorkHours.DatesWorkHours.Length).Select(rix => $"B{rix}"));
                        await spreadsheet.AddRowAsync(new[] {
                            new Cell($"{monthWorkHours.MonthName} {monthWorkHours.Year}"),
                            new Cell(new Formula(hoursSumForMonthFormula)),
                            new Cell(new Formula($"({hoursSumForMonthFormula})*{payRateCell}"), moneyStyleId) 
                        }
                        );
                        dateRowNum += monthWorkHours.DatesWorkHours.Length;
                    }

                    await spreadsheet.FinishAsync();
                }
                return outputStream.ToArray();
            }
        }
    }
}
