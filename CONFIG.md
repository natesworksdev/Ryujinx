## Config File

`Ryujinx.conf` should be present in executable folder (It's an *.ini file) following this format:

- `Logging_Enable_Info` *(bool)*

  Enable the Informations Logging.

- `Logging_Enable_Trace` *(bool)*

  Enable the Trace Logging (Enabled in Debug recommended).
  
- `Logging_Enable_Debug` *(bool)*

   Enable the Debug Logging (Enabled in Debug recommended).

- `Logging_Enable_Warn` *(bool)*

  Enable the Warning Logging (Enabled in Debug recommended).

- `Logging_Enable_Error` *(bool)*

  Enable the Error Logging (Enabled in Debug recommended).

- `Logging_Enable_Fatal` *(bool)*

  Enable the Fatal Logging (Enabled in Debug recommended).

- `Logging_Enable_Ipc` *(bool)*

  Enable the Ipc Message Logging.

- `Logging_Enable_LogFile` *(bool)*

  Enable writing the logging inside a Ryujinx.log file.
  
- `GamePad_Index` *(int)*

  The index of the Controller Device.
  
- `GamePad_Deadzone` *(float)*

  The deadzone of both analog sticks on the Controller.

- `GamePad_Enable` *(bool)*
  
  Whether or not to enable Controller Support.
  
- `Handheld_Device` *(String)*

  The specific Device to be the Emulated Handheld Device.
  
- `Player(1-8)_Device` *(String)*

  The specific Device to be the Emulated Player(1-8) Device.
  
- `PlayerUnknown_Device` *(String)*

  The specific Device to be the Emulated PlayerUnknown Device. (This should basically always be None)
  
- Valid Emulated Device Mappings
  - None = Disabled
  - Keyboard = The Keyboard Device
  - GamePad_X = X GamePad Configuration

- `Controls_Left_JoyConKeyboard_XX` *(int)*
  ```
  Controls_Left_JoyConKeyboard_Stick_Up (int)
  Controls_Left_JoyConKeyboard_Stick_Down (int)
  Controls_Left_JoyConKeyboard_Stick_Left (int)
  Controls_Left_JoyConKeyboard_Stick_Right (int)
  Controls_Left_JoyConKeyboard_Stick_Button (int)
  Controls_Left_JoyConKeyboard_DPad_Up (int)
  Controls_Left_JoyConKeyboard_DPad_Down (int)
  Controls_Left_JoyConKeyboard_DPad_Left (int)
  Controls_Left_JoyConKeyboard_DPad_Right (int)
  Controls_Left_JoyConKeyboard_Button_Minus (int)
  Controls_Left_JoyConKeyboard_Button_L (int)
  Controls_Left_JoyConKeyboard_Button_ZL (int)
  ```
  
  Keys of the Left Emulated Joycon, the values depend of the [OpenTK Enum Keys](https://github.com/opentk/opentk/blob/develop/src/OpenTK/Input/Key.cs).
  
  OpenTK uses a QWERTY layout, so pay attention if you use another Keyboard Layout.
  
  Ex: `Controls_Left_JoyConKeyboard_Button_Minus = 52` > Tab key (All Layout).

- `Controls_Right_JoyConKeyboard_XX` *(int)*
  ```
  Controls_Right_JoyConKeyboard_Stick_Up (int)
  Controls_Right_JoyConKeyboard_Stick_Down (int)
  Controls_Right_JoyConKeyboard_Stick_Left (int)
  Controls_Right_JoyConKeyboard_Stick_Right (int)
  Controls_Right_JoyConKeyboard_Stick_Button (int)
  Controls_Right_JoyConKeyboard_Button_A (int)
  Controls_Right_JoyConKeyboard_Button_B (int)
  Controls_Right_JoyConKeyboard_Button_X (int)
  Controls_Right_JoyConKeyboard_Button_Y (int)
  Controls_Right_JoyConKeyboard_Button_Plus (int)
  Controls_Right_JoyConKeyboard_Button_R (int)
  Controls_Right_JoyConKeyboard_Button_ZR (int)
  ```

  Keys of the right Emulated Joycon, the values depend of the [OpenTK Enum Keys](https://github.com/opentk/opentk/blob/develop/src/OpenTK/Input/Key.cs).
  
  OpenTK uses a QWERTY layout, so pay attention if you use another Keyboard Layout.
  
  Ex: `Controls_Right_JoyConKeyboard_Button_A = 83` > A key (QWERTY Layout) / Q key (AZERTY Layout).
  
- `Controls_Left_JoyConController_XX_X` *(String)*
  ```
  Controls_Left_JoyConController_Stick_X (String)
  Controls_Left_JoyConController_Stick_Button_X (String)
  Controls_Left_JoyConController_DPad_Up_X (String)
  Controls_Left_JoyConController_DPad_Down_X (String)
  Controls_Left_JoyConController_DPad_Left_X (String)
  Controls_Left_JoyConController_DPad_Right_X (String)
  Controls_Left_JoyConController_Button_Minus_X (String)
  Controls_Left_JoyConController_Button_L_X (String)
  Controls_Left_JoyConController_Button_ZL_X (String)
  ```
  
- `Controls_Right_JoyConController_XX_X` *(String)*
  ```
  Controls_Right_JoyConController_Stick_X (String)
  Controls_Right_JoyConController_Stick_Button_X (String)
  Controls_Right_JoyConController_Button_A_X (String)
  Controls_Right_JoyConController_Button_B_X (String)
  Controls_Right_JoyConController_Button_X_X (String)
  Controls_Right_JoyConController_Button_Y_X (String)
  Controls_Right_JoyConController_Button_Plus_X (String)
  Controls_Right_JoyConController_Button_R_X (String)
  Controls_Right_JoyConController_Button_ZR_X (String)
  ```

  The "X" is the Controller Configuration Number, to add more configurations, copy the first configuration, then increment the Number "X"
  change the Button Configuration as you wish.
  
- Valid Button Mappings
  - A = The A / Cross Button
  - B = The B / Circle Button
  - X = The X / Square Button
  - Y = The Y / Triangle Button
  - LStick = The Left Analog Stick when Pressed Down
  - RStick = The Right Analog Stick when Pressed Down
  - Start = The Start / Options Button
  - Back = The Select / Back / Share Button
  - RShoulder = The Right Shoulder Button
  - LShoulder = The Left Shoulder Button
  - RTrigger = The Right Trigger
  - LTrigger = The Left Trigger
  - DPadUp = Up on the DPad
  - DPadDown = Down on the DPad
  - DPadLeft = Left on the DPad
  - DpadRight = Right on the DPad
- Valid Joystick Mappings
  - LJoystick = The Left Analog Stick
  - RJoystick = The Right Analog Stick
  
On more obscure / weird controllers this can vary, so if this list doesn't work, trial and error will.

### How to configure Co-Op
To configure Co-Op you need to first have your Controller Configurations set up, mutliple of them if you're using multiple controllers.  Make sure to have the `GamePad_Index_X` variable correct for each one, this variable corresponds to each physical controller hooked up to your system, you can also use a Controller and a Keyboard as separate emulated players, this is entirely up to you.  Once you have chosen what you want to do, you then need to configure in the Configuration file.
Change the `Handheld_Device` to `None`, this is done as, multiple players do not work with the Handheld Device.
Change `Player1_Device` to either `Keyboard` or `GamePad_X`, the X being which Controller configuration you want to apply to this player, then go through all the players you want and do the same, changing them to the Input Device for your selected Player.  That's it, have fun!