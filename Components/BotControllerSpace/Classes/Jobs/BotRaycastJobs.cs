namespace SAIN.Components
{
    public class BotRaycastJobs(SAINBotController botController) : SAINControllerBase(botController)
    {
        public VisionRaycastJob VisionJob { get; } = new VisionRaycastJob(botController);
        public EnemyPlaceRaycastJob EnemyPlaceJob { get; } = new EnemyPlaceRaycastJob(botController);

        public EnemyPathVisibilityRaycastJob PathVisibilityJob { get; } = new EnemyPathVisibilityRaycastJob(botController);
        public RandomVisiblePointGeneratorJob RandomVisiblePointGeneratorJob { get; } = new RandomVisiblePointGeneratorJob(botController);

        public void Update()
        {
        }

        public void Dispose()
        {
            VisionJob.Dispose();
            EnemyPlaceJob.Dispose();
            PathVisibilityJob.Dispose();
            RandomVisiblePointGeneratorJob.Dispose();
        }
    }
}