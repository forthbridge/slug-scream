namespace SlugScream
{
    internal class Enums
    {
        public static void RegisterValues()
        {
            Sounds.RegisterValues();
        }

        public static void UnregisterValues()
        {
            Sounds.UnregisterValues();
        }

        public class Sounds
        {
            public static SoundID? SlugScream;

            public static void RegisterValues()
            {
                SlugScream = new SoundID("SlugScream", true);
            }

            public static void UnregisterValues()
            {
                if (SlugScream != null) SlugScream = null;
            }
        }
    }
}
