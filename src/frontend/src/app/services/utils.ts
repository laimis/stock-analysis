import {formatDistance, parse} from "date-fns";

export function GetErrors(err: any): string[] {
    try {

        if (err.status && err.status === 401) {
            return ["Unauthorized, please login to continue"]
        }

        if (err.error === undefined) {
            console.error("Failed to get errors: " + err)
            return [err.message ?? "Unknown error"]
        }

        var objToMap = err.error.errors
        if (objToMap === undefined) {
            objToMap = err.error
        }

        if (typeof (objToMap) === 'string') {
            return [objToMap]
        }

        return Object.keys(objToMap).map<string>(v => {
            return Object.getOwnPropertyDescriptor(objToMap, v).value
        })
    } catch (e) {
        console.log("Failed to get errors: " + e)
        return []
    }
}

export function toggleVisuallyHidden(element: HTMLElement) {
    const className = 'visually-hidden';
    if (element.classList.contains(className)) {
        element.classList.remove(className);
    } else {
        element.classList.add(className);
    }
}

export function hideElement(element: HTMLElement) {
    const className = 'visually-hidden';
    if (!element.classList.contains(className)) {
        element.classList.add(className);
    }
}

export function showElement(element: HTMLElement) {
    const className = 'visually-hidden';
    if (element.classList.contains(className)) {
        element.classList.remove(className);
    }
}

// export an array of key value pairs representing strategies
export function GetStockStrategies(): { key: string, value: string }[] {
    return [
        {key: "10x", value: "10x"},
        {key: "descendingchannelbreakthrough", value: "Descending Channel Breakthrough"},
        {key: "discretionary", value: "Discretionary"},
        {key: "channelbottom", value: "Channel Bottom"},
        {key: "leadingindustry", value: "Leading Industry"},
        {key: "longterm", value: "Long Term"},
        {key: "longterminterest", value: "Long Term Interest"},
        {key: "newhigh", value: "New High"},
        {key: "newhighpullback", value: "New High Pullback"},
        {key: "postearnings", value: "Post Earnings"},
        {key: "postearningsnewhigh", value: "Post Earnings New High"},
        {key: "recovery", value: "Recovery"},
        {key: "resistancebreakthrough", value: "Resistance Breakthrough"},
        {key: "shortnewlow", value: "Short, New Low"},
        {key: "shortsuspectrunup", value: "Short, Suspect Runup"},
        {key: "shortweakindustry", value: "Short, Weak Industry"},
        {key: "shortweakness", value: "Short, Weakness"},
        {key: "weeklypullbreak", value: "Weekly Pull/Break"},
        {key: "weeklypullbreakdelayed", value: "Weekly Pull/Break Delayed"},
    ]
}

export function GetOptionStrategies() : { key: string, value: string }[] {
    return [
        {key: "short", value: "Short"},
        {key: "long", value: "Long"},
        {key: "channelbottombearish", value: "Channel Bottom Bearish"},
        {key: "channelbottombullish", value: "Channel Bottom Bullish"},
        {key: "descendingchannelbreakout", value: "Descending Channel Breakout"},
        {key: "insanelyovervalued", value: "Insanely Overvalued"},
        {key: "hugerunup", value: "Huge Runup"},
        {key: "priceRunUpVsObv", value: "Price Run Up Vs OBV"},
        {key: "speculatingonrecovery", value: "Speculating On Recovery"},
    ]
}

export function isLongTermStrategy(strategy: string) {
    return strategy === "longterm" || strategy === "longterminterest"
}

export function parseDate(date: string) {
    // parses in a way where it does not assume that date string like 2024-10-01 is in UTC
    // it will assume that the date string is in the local timezone and return date object in local timezone
    return parse(date, 'yyyy-MM-dd', new Date())
}

export function convertToLocalTime(date: Date) {
    return new Date(date.getTime() - date.getTimezoneOffset() * 60000)
}

export function humanFriendlyDuration(numberOfDays: number) {
    // use date-fns to convert number of days to human friendly duration
    let today = new Date()
    let numberOfDaysAgoAsDate = new Date(today.setDate(today.getDate() - numberOfDays))
    return formatDistance(numberOfDaysAgoAsDate, new Date(), {includeSeconds: false, addSuffix: true})
}
