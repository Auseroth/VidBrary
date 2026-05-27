!include "FileFunc.nsh"
!include "x64.nsh"
!include "MUI2.nsh"

!define APPNAME "VidBrary"
!define EXENAME "VidBrary.exe"
!define INSTALLDIR "$PROGRAMFILES\${APPNAME}"
!define MUI_NO_DEFAULT_BUTTONS
!define MUI_NO_DEFAULT_BRANDING

Outfile "C:\\temp file transfer\\9.VisualStudio\\exe wrapper scripts\\NSIS Output\\${APPNAME}_Install.exe"
InstallDir "${INSTALLDIR}"

RequestExecutionLevel admin

!define MUI_ABORTWARNING
!define MUI_ICON "C:\\temp file transfer\\9.VisualStudio\\field testing\\mediacatalog\\mediacatalog\\bin\\Release\\net8.0-windows\\publish\\VidBrary\\libDiscPlay3 (2).ico"

;--------------------------------
; Modern UI Language Strings (English)
;--------------------------------
LangString MUI_TEXT_WELCOME_INFO_TITLE 1033 "Welcome to the VidBrary Setup Wizard"
LangString MUI_TEXT_WELCOME_INFO_TEXT 1033 "This wizard will guide you through the installation of VidBrary."
LangString MUI_TEXT_INSTALLING_TITLE 1033 "Installing"
LangString MUI_TEXT_INSTALLING_SUBTITLE 1033 "Please wait while VidBrary is being installed."
LangString MUI_TEXT_FINISH_TITLE 1033 "Installation Complete"
LangString MUI_TEXT_FINISH_SUBTITLE 1033 "Setup has finished installing VidBrary."
LangString MUI_TEXT_ABORT_TITLE 1033 "Installation Aborted"
LangString MUI_TEXT_ABORT_SUBTITLE 1033 "The installation was not completed."
LangString MUI_BUTTONTEXT_FINISH 1033 "Finish"
LangString MUI_TEXT_FINISH_INFO_TITLE 1033 "VidBrary Setup Finished"
LangString MUI_TEXT_FINISH_INFO_TEXT 1033 "Click Finish to exit the Setup Wizard."
LangString MUI_TEXT_FINISH_INFO_REBOOT 1033 "You must restart your computer to complete the installation."
LangString MUI_TEXT_FINISH_REBOOTNOW 1033 "Restart Now"
LangString MUI_TEXT_FINISH_REBOOTLATER 1033 "Restart Later"

!insertmacro MUI_PAGE_WELCOME
Page custom OptionsPageShow OptionsPageLeave
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_RUN "$INSTDIR\${EXENAME}"
!define MUI_FINISHPAGE_RUN_TEXT "Run VidBrary"
!define MUI_FINISHPAGE_RUN_FUNCTION "FinishPageRunFunction"
!define MUI_FINISHPAGE_SHOWREADME "$INSTDIR\README.md"
!define MUI_FINISHPAGE_SHOWREADME_TEXT "View README file"
!define MUI_FINISHPAGE_SHOWREADME_FUNCTION "FinishPageShowReadmeFunction"
!define MUI_FINISHPAGE_SHOWREADME_CHECKED
!insertmacro MUI_PAGE_FINISH

Function FinishPageRunFunction
    ExecShell "open" "$INSTDIR\${EXENAME}"
FunctionEnd

Function FinishPageShowReadmeFunction
    ExecShell "open" "$INSTDIR\README.md"
FunctionEnd

Var STARTUP_CHECKED
Var DESKTOP_CHECKED

;--------------------------------
; Version Information
;--------------------------------
VIProductVersion "1.0.0.0"
VIAddVersionKey "CompanyName" "City of Newport News - Public Safety IT"
VIAddVersionKey "LegalCopyright" "© 2025 City of Newport News"
VIAddVersionKey "FileVersion" "1.0.0.0"
VIAddVersionKey "ProductVersion" "1.0.0.0"
VIAddVersionKey "Author" "Austin Sharman"
VIAddVersionKey "FileDescription" "App to launch and monitor any number of apps, designed to be ran as a custom shell app Written By Austin Sharman"
VIAddVersionKey "InternalName" "${APPNAME}"
VIAddVersionKey "Trademarks" "City of Newport News"

;--------------------------------
; Installer Icon
;--------------------------------
Icon "C:\\temp file transfer\\9.VisualStudio\\field testing\\mediacatalog\\mediacatalog\\bin\\Release\\net8.0-windows\\publish\\VidBrary\\libDiscPlay3 (2).ico"


Function OptionsPageShow
    !insertmacro MUI_HEADER_TEXT "Installation Options" "Choose additional tasks:"
    nsDialogs::Create 1018
    Pop $0
    ${If} $0 == error
        Abort
    ${EndIf}
    ${NSD_CreateCheckbox} 0u 20u 100% 12u "Add to Startup (all users)"
    Pop $1
    ${NSD_SetState} $1 $STARTUP_CHECKED
    ${NSD_CreateCheckbox} 0u 40u 100% 12u "Add shortcut to Public Desktop"
    Pop $2
    ${NSD_SetState} $2 $DESKTOP_CHECKED
    nsDialogs::Show
FunctionEnd

Function OptionsPageLeave
    ${NSD_GetState} $1 $STARTUP_CHECKED
    ${NSD_GetState} $2 $DESKTOP_CHECKED
FunctionEnd

Section "Install"

    SetOutPath "$INSTDIR"

    ; Copy all files EXCEPT .pdb and .pdb-related files
    File /r /x "*.pdb" "C:\\temp file transfer\\9.VisualStudio\\field testing\\mediacatalog\\mediacatalog\\bin\\Release\\net8.0-windows\\publish\\VidBrary\\*.*"

    ; Write uninstaller
    WriteUninstaller "$INSTDIR\Uninstall.exe"


; Start Menu shortcut
    SetShellVarContext all
    CreateDirectory "$SMPROGRAMS\\${APPNAME}"
    CreateShortcut "$SMPROGRAMS\\${APPNAME}\\${APPNAME}.lnk" "$INSTDIR\\${EXENAME}"

    ; Add to Startup (all users) if selected
    ${If} $STARTUP_CHECKED == 1
        SetShellVarContext all
        CreateShortcut "$SMSTARTUP\${APPNAME}.lnk" \
  "$INSTDIR\${EXENAME}" \
  "" \
  "$INSTDIR\${EXENAME}" \
  0 \
  SW_SHOWNORMAL \
  "" \
  "${APPNAME}"
    ${EndIf}

    ; Add shortcut to Public Desktop if selected
    ${If} $DESKTOP_CHECKED == 1
        SetShellVarContext all
        CreateShortcut "$Desktop\${APPNAME}.lnk" "$INSTDIR\${EXENAME}"
    ${EndIf}

    ; Write registry info
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayName" "${APPNAME}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "UninstallString" "$\"$INSTDIR\Uninstall.exe$\""
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayIcon" "$INSTDIR\${EXENAME}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayVersion" "1.2.0.1"
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "NoModify" 1
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "NoRepair" 1



SectionEnd



Section "Uninstall"

    ; Remove files
    Delete "$INSTDIR\*.*"
    RMDir /r "$INSTDIR"


    ; delete start menu shortcuts
    setshellvarcontext all
    Delete "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk"
    RMDir "$SMPROGRAMS\${APPNAME}"

    ; Remove from Startup
    SetShellVarContext all
    Delete "$SMSTARTUP\${APPNAME}.lnk"

    ; Remove from Public Desktop
    SetShellVarContext all
    Delete "$Desktop\${APPNAME}.lnk"

    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"


SectionEnd
