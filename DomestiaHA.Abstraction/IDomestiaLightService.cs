namespace DomestiaHA.Abstraction
{
    public interface IDomestiaLightService
    {

        /// <summary>
        /// Set a light brigthness value
        /// </summary>
        /// <param name="lightId">lightId</param>
        /// <param name="brigthness">brightness value [0, 255]</param>
        public void SetBrigthness(string lightId, int brigthness);

        /// <summary>
        /// Get a light brigthness value
        /// </summary>
        /// <param name="lightId">lightId</param>
        /// <returns>brigthness [0,255]</returns>
        public int GetBrigthness(string lightId);

    }
}
