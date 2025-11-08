using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public interface IResettablePlayer
{
    /// <summary>
    /// Resets the character's data to its default values.
    /// </summary>
    void ResetCharacter();
    void InitializeDefaults();
}
