namespace AISEG.DigitalTwin.Conveyor;

public class ConveyorParameters
{
    // =========================================================
    // PHYSICAL DIMENSIONS
    // =========================================================

    // Conveyor length from the NTPC proposal.
    public float Length { get; set; } = 10.0f;

    // Conveyor width selected for our simulation.
    public float Width { get; set; } = 1.4f;

    // =========================================================
    // CONVEYOR SPEED
    // =========================================================

    // Speed in metres per second.
    public float Speed { get; set; } = 0.7f;

    // =========================================================
    // CAPACITY
    // =========================================================

    // Maximum item weight in kilograms.
    public float MaximumItemWeight { get; set; } = 25.0f;

    // =========================================================
    // CONVEYOR STATE
    // =========================================================

    public bool IsRunning { get; set; } = true;

    // =========================================================
    // CONVEYOR DIRECTION
    // =========================================================

    // +1 = forward
    // -1 = reverse

    public int Direction { get; set; } = 1;

    // =========================================================
    // HELPER PROPERTY
    // =========================================================

    // Returns the actual velocity of the conveyor.
    //
    // Example:
    //
    // Speed = 0.7
    // Direction = 1
    //
    // Velocity = +0.7 m/s
    //
    // If Direction = -1:
    //
    // Velocity = -0.7 m/s

    public float Velocity
    {
        get
        {
            if (!IsRunning)
            {
                return 0.0f;
            }

            return Speed * Direction;
        }
    }
}