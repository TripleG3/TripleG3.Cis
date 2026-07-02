namespace TripleG3.Cis.Examples;

/// <summary>
/// Identifies the steps in the example workflow.
/// </summary>
public enum ExampleServiceSteps
{
    /// <summary>
    /// The workflow has not started.
    /// </summary>
    None,

    /// <summary>
    /// The workflow is at the first step.
    /// </summary>
    Step1,

    /// <summary>
    /// The workflow is at the second step.
    /// </summary>
    Step2,

    /// <summary>
    /// The workflow is at the third step.
    /// </summary>
    Step3,

    /// <summary>
    /// The workflow has completed all steps.
    /// </summary>
    Complete
}
