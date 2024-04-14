interface EuisMainModel {
    currentTime: string,
    currentDate: string,
    monitorIdentifierText: string,
    monitorNumber: number
}

declare var __euis_main: EuisMainModel
declare var __euisReady: Promise<boolean>

type ApplicationInjectionData = {
    appName: string,
    displayName: string,
    jsUrl: string,
    iconUrl: string
}