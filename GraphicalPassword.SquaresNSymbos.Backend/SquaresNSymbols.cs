namespace GraphicalPassword.SquaresNSymbos.Backend
{
    public class SquaresNSymbols
    {
        public static bool ComparePasswords(string correct, string given)
        {
            if (correct.Length != 10)
                throw new ArgumentException("Provided correct password is of invalid format, " +
                    "please provide the same password that you received from frontend side");

            if (given.Length != 20)
                throw new ArgumentException("Provided given password is of invalid format, " +
                    "please provide the same password that you received from frontend side");

            var correctParts = Enumerable.Range(0, correct.Length / 2)
                .Select(i => correct.Substring(i * 2, 2)).ToArray();
            var givenParts = Enumerable.Range(0, given.Length / 4)
                .Select(i => given.Substring(i * 4, 4)).ToArray();

            for (int i = 0; i < correctParts.Length; i++)
            {
                if (!givenParts[i].Contains(correctParts[i]))
                    return false;
            }

            return true;
        }
    }
}