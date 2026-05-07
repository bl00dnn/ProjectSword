using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

internal static class SergiusInput
{
    public static Vector2 ReadMove()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 move = Vector2.zero;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 stick = gamepad.leftStick.ReadValue();
            if (stick.sqrMagnitude > move.sqrMagnitude)
            {
                move = stick;
            }
        }

        return Vector2.ClampMagnitude(move, 1f);
#else
        return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
#endif
    }

    public static Vector2 ReadLook()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 look = Vector2.zero;

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            look += mouse.delta.ReadValue();
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            look += gamepad.rightStick.ReadValue() * 120f * Time.deltaTime;
        }

        return look;
#else
        return new Vector2(Input.GetAxis("Mouse X") * 12f, Input.GetAxis("Mouse Y") * 12f);
#endif
    }

    public static bool ReadSprint()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        return (keyboard != null && keyboard.leftShiftKey.isPressed) ||
               (gamepad != null && gamepad.leftStickButton.isPressed);
#else
        return Input.GetKey(KeyCode.LeftShift);
#endif
    }

    public static bool ReadJump()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        return (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) ||
               (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }
}
