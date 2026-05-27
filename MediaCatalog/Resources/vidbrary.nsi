!define APPNAME "VidBrary"
!define EXENAME "VidBrary.exe"
!define INSTALLDIR "$PROGRAMFILES\${APPNAME}"
!define MUI_NO_DEFAULT_BUTTONS
!define MUI_NO_DEFAULT_BRANDING
!include "FileFunc.nsh"
!include "x64.nsh"
!include "MUI2.nsh"
Var MAINTENANCE_MODE
Var MAINTENANCE_CHOICE
Function CloseAppIfRunning
    ; Force kill VidBrary.exe using taskkill (runs as admin, most reliable)
    nsExec::ExecToLog 'taskkill /F /IM "VidBrary.exe"'
    Sleep 1000
FunctionEnd

Function WelcomePagePre
    ; Skip welcome page when app is already installed (maintenance mode)
    StrCmp $MAINTENANCE_MODE "" show_welcome
    Abort
    show_welcome:
FunctionEnd

Function MaintenancePageLeave
    ${NSD_GetState} $1 $MAINTENANCE_CHOICE
    ${If} $MAINTENANCE_CHOICE == ${BST_CHECKED}
        StrCpy $MAINTENANCE_CHOICE "update"
        ; "update" falls through — continues to install pages (Options/License skipped by callbacks)
    ${Else}
        ${NSD_GetState} $2 $MAINTENANCE_CHOICE
        ${If} $MAINTENANCE_CHOICE == ${BST_CHECKED}
            ; --- UNINSTALL: handle immediately before showing any more pages ---
            StrCpy $MAINTENANCE_CHOICE "uninstall"
            MessageBox MB_YESNO|MB_ICONQUESTION "Are you sure you want to uninstall ${APPNAME}?" IDYES mpl_confirmed
            Abort  ; No — stay on maintenance page
            mpl_confirmed:
            MessageBox MB_YESNO|MB_ICONQUESTION "Also delete the ProgramData\${APPNAME} folder (saved settings and data)?" IDYES mpl_del_pd IDNO mpl_skip_pd
            mpl_del_pd:
                RMDir /r "$PROGRAMDATA\${APPNAME}"
            mpl_skip_pd:
            Call CloseAppIfRunning
            ExecWait '"$INSTDIR\Uninstall.exe"'
            Quit
        ${Else}
            ; --- CANCEL: close installer immediately ---
            StrCpy $MAINTENANCE_CHOICE "cancel"
            Quit
        ${EndIf}
    ${EndIf}
FunctionEnd

Function LicensePageShow
    ; If not installed, show license normally
    StrCmp $MAINTENANCE_MODE "${APPNAME}" 0 show_license
    ; If installed and choice is update, uninstall, or cancel — skip license
    StrCmp $MAINTENANCE_CHOICE "update" skip_license
    StrCmp $MAINTENANCE_CHOICE "uninstall" skip_license
    StrCmp $MAINTENANCE_CHOICE "cancel" skip_license
    Goto show_license
    skip_license:
        Abort
    show_license:
FunctionEnd

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



!define MUI_PAGE_CUSTOMFUNCTION_PRE WelcomePagePre
!insertmacro MUI_PAGE_WELCOME
!undef MUI_PAGE_CUSTOMFUNCTION_PRE


Page custom MaintenancePageShow MaintenancePageLeave

Function .onInit
    ; No page logic here, just set variable
    ReadRegStr $MAINTENANCE_MODE HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayName"
FunctionEnd
Function MaintenancePageShow
        ; Only show if installed, else skip
        StrCmp $MAINTENANCE_MODE "${APPNAME}" 0 skip
        !insertmacro MUI_HEADER_TEXT "VidBrary Maintenance" "Choose an action:"
        nsDialogs::Create 1018
        Pop $0
        ${If} $0 == error
                Abort
        ${EndIf}
        ${NSD_CreateRadioButton} 0u 20u 100% 12u "Update/Repair VidBrary"
        Pop $1
        ${NSD_SetState} $1 ${BST_CHECKED}
        ${NSD_CreateRadioButton} 0u 40u 100% 12u "Uninstall VidBrary"
        Pop $2
        ${NSD_CreateRadioButton} 0u 60u 100% 12u "Cancel"
        Pop $3
        nsDialogs::Show
        Return
    skip:
        Abort
FunctionEnd

Page custom OptionsPageShow OptionsPageLeave
!define MUI_LICENSEPAGE_CHECKBOX
!define MUI_LICENSEPAGE_SCROLLTOP
!define MUI_PAGE_CUSTOMFUNCTION_SHOW LicensePageShow
!insertmacro MUI_PAGE_LICENSE "C:\\temp file transfer\\9.VisualStudio\\field testing\\MediaCatalog\\MediaCatalog\\bin\\Release\\net8.0-windows\\publish\\VidBrary\\Resources\\TERMS_OF_USE.txt"
!undef MUI_PAGE_CUSTOMFUNCTION_SHOW
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
VIProductVersion "1.2.0.0"
VIAddVersionKey "CompanyName" "City of Newport News - Public Safety IT"
VIAddVersionKey "LegalCopyright" "© 2025 City of Newport News"
VIAddVersionKey "FileVersion" "1.2.0.0"
VIAddVersionKey "ProductVersion" "1.2.0.0"
VIAddVersionKey "Author" "Austin Sharman"
VIAddVersionKey "FileDescription" "App to launch and monitor any number of apps, designed to be ran as a custom shell app Written By Austin Sharman"
VIAddVersionKey "InternalName" "${APPNAME}"
VIAddVersionKey "Trademarks" "City of Newport News"

;--------------------------------
; Installer Icon
;--------------------------------
Icon "C:\\temp file transfer\\9.VisualStudio\\field testing\\mediacatalog\\mediacatalog\\bin\\Release\\net8.0-windows\\publish\\VidBrary\\libDiscPlay3 (2).ico"


Function OptionsPageShow
    ; Skip options page in maintenance mode (only update reaches here, no options needed)
    StrCmp $MAINTENANCE_MODE "${APPNAME}" 0 show_options
    StrCmp $MAINTENANCE_CHOICE "update" skip_options
    Goto show_options
    skip_options:
        Abort
    show_options:
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
    ; Branch: maintenance mode (update) vs fresh install
    StrCmp $MAINTENANCE_MODE "${APPNAME}" is_maintenance
    Goto do_install

    is_maintenance:
        ; Only "update/repair" reaches here — uninstall/cancel were handled in MaintenancePageLeave
        Call CloseAppIfRunning
        Goto update_only

    update_only:
        ; Only run instfiles page for update/repair
        SetOutPath "$INSTDIR"
        File /r /x "*.pdb" "C:\temp file transfer\9.VisualStudio\field testing\mediacatalog\mediacatalog\bin\Release\net8.0-windows\publish\VidBrary\*.*"
        WriteUninstaller "$INSTDIR\Uninstall.exe"
        SetShellVarContext all
        CreateDirectory "$SMPROGRAMS\${APPNAME}"
        CreateShortcut "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk" "$INSTDIR\VidBrary.exe"
        ${If} $STARTUP_CHECKED == 1
            SetShellVarContext all
            CreateShortcut "$SMSTARTUP\${APPNAME}.lnk" "$INSTDIR\VidBrary.exe"
        ${EndIf}
        ${If} $DESKTOP_CHECKED == 1
            SetShellVarContext all
            CreateShortcut "$Desktop\${APPNAME}.lnk" "$INSTDIR\VidBrary.exe"
        ${EndIf}
        WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayName" "${APPNAME}"
        WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "UninstallString" "$\"$INSTDIR\Uninstall.exe$\""
        WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayIcon" "$INSTDIR\VidBrary.exe"
        WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayVersion" "1.3.1.1"
        WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "NoModify" 1
        WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "NoRepair" 1
        Goto end_maint
    do_install:

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
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "DisplayVersion" "1.3.2.1"
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "NoModify" 1
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}" "NoRepair" 1
    end_maint:
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
