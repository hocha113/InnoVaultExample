using InnoVault.DataModules;
using InnoVaultExample.Core;

namespace InnoVaultExample.Content.Academy.Persistence
{
    // Showcase: Student Progress Module (WORLD-301)
    // Demonstrates: DataModule course flags, reflection save, Tutor-ready completion count
    // Wiki: https://innovault.wiki/cn/persistence/data-modules/
    // Source: InnoVaultExample/Content/Academy/Persistence/AcademyProgressModule.cs

    public class AcademyProgressModule : DataModule
    {
        public bool CoursePrt101Complete { get; set; }
        public bool CourseCombat201Complete { get; set; }
        public bool CourseWorld301Complete { get; set; }

        public bool WorkbenchVisited { get; set; }
        public bool TurretVisited { get; set; }
        public bool LockerVisited { get; set; }

        public int CompletedCourseCount {
            get {
                int count = 0;
                if (CoursePrt101Complete) count++;
                if (CourseCombat201Complete) count++;
                if (CourseWorld301Complete) count++;
                return count;
            }
        }

        public void MarkWorkbenchVisited() {
            if (!AcademyAuthority.HasAuthority()) {
                return;
            }

            WorkbenchVisited = true;
            TryCompleteWorld301();
        }

        public void MarkTurretVisited() {
            if (!AcademyAuthority.HasAuthority()) {
                return;
            }

            TurretVisited = true;
            TryCompleteWorld301();
        }

        public void MarkLockerVisited() {
            if (!AcademyAuthority.HasAuthority()) {
                return;
            }

            LockerVisited = true;
            TryCompleteWorld301();
        }

        public void MarkCoursePrt101Complete() {
            if (!AcademyAuthority.HasAuthority()) {
                return;
            }

            CoursePrt101Complete = true;
        }

        public void MarkCourseCombat201Complete() {
            if (!AcademyAuthority.HasAuthority()) {
                return;
            }

            CourseCombat201Complete = true;
        }

        private void TryCompleteWorld301() {
            if (WorkbenchVisited && TurretVisited && LockerVisited) {
                CourseWorld301Complete = true;
            }
        }
    }
}
