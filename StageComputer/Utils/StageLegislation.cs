using OpenCloseDays.Models;
using System.Globalization;

namespace StageComputer.Utils
{
    public class StageLegislation
    {
		public const float LEGAL_PAYRATE_PLAFOND_RATIO = 0.15f;
		public const string ENTERPRISE_TYPE_PUBLIC = "public";
        public const string ENTERPRISE_TYPE_PRIVATE = "private";
        public const string IN_FRANCE = "fr";
        public const string OUTSIDE_FRANCE = "foreign";
        public const string TRAINING_MEDICAL = "medical";
        public const string TRAINING_OTHER = "other";
        private const string StagePublic_MinPayRate_Message =
            "Dans les organismes publics, le montant horaire est de {0}€ pour les stages de plus de {1} heures.";
        private const string StagePrivate_MinPayRate_Message =
            "Le montant minimal est de {0}€ de l'heure pour les stages de plus de {1} heures. Si le montant horaire de votre gratification est plus élevé, veuillez le corriger directement dans le champ « Montant horaire ».";
        private const string StagePublic_MaxPayRate_Message =
            "Attention ! Dans les organismes publics, le montant horaire ne peut être supérieur à {0}€.";
        private const string StageMedical_Message =
            "Les stages dans le cadre des formations d’auxiliaires médicaux ne sont pas gratifiés";
        public const int LongStage_Hours = 308;
        public const int MaxAllowed_Hours = 924;
		private const string StageTooLong_Message = "La durée du stage est limitée à {0} heures de présence effective (équivalent de 6 mois temps plein) dans un même organisme d’accueil sur la même année universitaire. A noter : un stage à temps partiel peut se dérouler sur une durée calendaire de plus de 6 mois dans la limite du calendrier universitaire et des {0} heures.";

		public static bool IsInAllowedHours(float hours) => hours <= MaxAllowed_Hours;

        public static DateTime? GetLongStageStartDate(WorkDaysComputationResult workDaysComputationResult)
        {
            var totalHours = 0.0f;
            DateWorkHours lastDayWithHours = null;
            foreach (var dayWorkHours in workDaysComputationResult.DatesWorkHours)
            {
                if (totalHours + dayWorkHours.WorkedHours > LongStage_Hours)
                    return lastDayWithHours?.Date;

                if (dayWorkHours.WorkedHours > 0)
                    lastDayWithHours = dayWorkHours;

                totalHours += dayWorkHours.WorkedHours;
            }
            return null;
        }

        public static async Task EnsureLegislationForPayRateAsync(
            PlafondSecuFetcher plafondSecuFetcher,
            DateTime startStageDate,
            float stagePayRate,
            string enterpriseType, string countryType, string trainingType,
            float stageHours,
            Func<float, string, Task> updatePayRateIfNotInLaw)
        {
            if (OUTSIDE_FRANCE == countryType)
                return;

            var CURRENT_LEGAL_PAYRATE = await plafondSecuFetcher.GetLegalStagePayRateAsync(startStageDate);

            if (TRAINING_MEDICAL == trainingType )
            {
                if (stagePayRate > 0)
                {
                    await updatePayRateIfNotInLaw(0,
                        string.Format(
                            StageMedical_Message,
                        CURRENT_LEGAL_PAYRATE, LongStage_Hours));
                }
                return;
            }

            if (stageHours > LongStage_Hours)
            {
                if (ENTERPRISE_TYPE_PUBLIC == enterpriseType)
                {
                    if (stagePayRate < CURRENT_LEGAL_PAYRATE)
                    {
                        await updatePayRateIfNotInLaw(CURRENT_LEGAL_PAYRATE,
                            string.Format(
                                StagePublic_MinPayRate_Message,
                            CURRENT_LEGAL_PAYRATE, LongStage_Hours));
                    }
                }
                else
                {
                    if (stagePayRate < CURRENT_LEGAL_PAYRATE)
                    {
                        await updatePayRateIfNotInLaw(CURRENT_LEGAL_PAYRATE,
                            string.Format(
                                StagePrivate_MinPayRate_Message,
                            CURRENT_LEGAL_PAYRATE, LongStage_Hours));
                    }
                }
            }
            if (ENTERPRISE_TYPE_PUBLIC == enterpriseType)
            {
                if (stagePayRate > CURRENT_LEGAL_PAYRATE)
                {
                    await updatePayRateIfNotInLaw(CURRENT_LEGAL_PAYRATE,
                        String.Format(
                            StagePublic_MaxPayRate_Message,
                            CURRENT_LEGAL_PAYRATE, LongStage_Hours));
                }
            }
        }

        public static string GetTooLongStageHoursMessage()
            => string.Format(StageTooLong_Message, MaxAllowed_Hours);
	}
}
