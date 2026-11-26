using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ITDigitacao.Core
{
    public interface IMonotonicClock
    {
        TimeSpan Now { get; }
    }

    public sealed class SessionClock : IMonotonicClock
    {
        private readonly Stopwatch clock = Stopwatch.StartNew();
        public TimeSpan Now => clock.Elapsed;
    }

    public sealed class SessionManager
    {
        private readonly IMonotonicClock clock;
        private readonly Dictionary<string, int> bestCharacters = new Dictionary<string, int>();
        private readonly HashSet<string> completed = new HashSet<string>();
        private TimeSpan? started;
        private TimeSpan? ended;
        private int correctAttempts;
        private int errors;
        private int corrected;

        public SessionManager(int minutes, IMonotonicClock clock)
        {
            if (minutes < 5 || minutes > 30 || minutes % 5 != 0)
                throw new ArgumentOutOfRangeException(nameof(minutes));
            this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
            Duration = TimeSpan.FromMinutes(minutes);
        }

        public TimeSpan Duration { get; }
        public Activity Activity { get; private set; }
        public TypingEngine Engine { get; private set; }
        public bool Started => started.HasValue;
        public bool Finished => ended.HasValue;
        public TimeSpan Elapsed => !Started ? TimeSpan.Zero :
            TimeSpan.FromTicks(Math.Min(Duration.Ticks, ((ended ?? clock.Now) - started.Value).Ticks));
        public TimeSpan Remaining => Duration - Elapsed;
        public int CompletedCount => completed.Count;
        public string LastCompletedId { get; private set; }
        public int TotalErrors => errors + (Engine?.ErrorAttempts ?? 0);
        public int CurrentErrors => Engine?.CurrentErrors ?? 0;
        public int CorrectedErrors => corrected + (Engine?.ResolvedErrors ?? 0);
        public double? Accuracy
        {
            get
            {
                int correct = correctAttempts + (Engine?.CorrectAttempts ?? 0);
                int total = correct + TotalErrors;
                return total == 0 ? (double?)null : 100.0 * correct / total;
            }
        }
        public double? WordsPerMinute
        {
            get
            {
                var elapsed = Elapsed;
                if (elapsed < TimeSpan.FromSeconds(10))
                    return null;
                var count = bestCharacters.Where(pair => pair.Key != Activity?.Id).Sum(pair => pair.Value);
                if (Activity != null)
                    count += Math.Max(GetBest(Activity.Id), Engine.CurrentCorrect);
                return count / 5.0 / elapsed.TotalMinutes;
            }
        }

        public bool Open(Activity activity)
        {
            if (activity == null)
                throw new ArgumentNullException(nameof(activity));
            Tick();
            if (Finished)
                return false;
            if (Activity?.Id == activity.Id)
                return true;
            CloseActivity();
            Activity = activity;
            Engine = new TypingEngine(activity.Text);
            return true;
        }

        public void Apply(string text) => ApplyEdit(text, -1, true);

        public void ApplyEdit(string text, int offset, bool countAttempts)
        {
            Tick();
            if (Finished || Engine == null)
                return;
            if (Engine.ApplyEdit(text, offset, countAttempts) > 0 && !Started)
                started = clock.Now;
        }

        public bool CompleteActivity()
        {
            Tick();
            if (Finished || Engine == null || !Engine.Complete)
                return false;
            completed.Add(Activity.Id);
            LastCompletedId = Activity.Id;
            CloseActivity();
            return true;
        }

        public void Tick()
        {
            if (Started && !Finished && Elapsed >= Duration)
                End();
        }

        public void End()
        {
            if (!Finished)
                ended = clock.Now;
        }

        private int GetBest(string id) => bestCharacters.TryGetValue(id, out int count) ? count : 0;

        private void CloseActivity()
        {
            if (Engine == null)
                return;
            bestCharacters[Activity.Id] = Math.Max(GetBest(Activity.Id), Engine.CurrentCorrect);
            correctAttempts += Engine.CorrectAttempts;
            errors += Engine.ErrorAttempts;
            corrected += Engine.ResolvedErrors;
            Activity = null;
            Engine = null;
        }
    }
}
