namespace ITDigitacao.Core
{
    public static class SessionAwards
    {
        public static int Stars(int evaluated, double? accuracy)
        {
            if (evaluated < 100 || !accuracy.HasValue)
                return 0;
            return accuracy >= 95 ? 3 : accuracy >= 80 ? 2 : 1;
        }

        public static bool FirstMission(int completed) => completed >= 1;
        public static bool PerfectAim(int evaluated, double? accuracy) => Stars(evaluated, accuracy) == 3;
        public static bool SuperPractitioner(int completed) => completed >= 3;
    }
}
