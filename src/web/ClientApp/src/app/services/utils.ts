export function GetErrors(err:any): string[] {
  try{

    if (err.status && err.status === 401) {
      return ["Unauthorized, please login to continue"]
    }

    if (err.error === undefined) {
      console.error("Failed to get errors: " + err)
      return [err.message ?? "Unknown error"]
    }

    var objToMap = err.error.errors
    if (objToMap === undefined)
    {
      objToMap = err.error
    }

    if (typeof(objToMap) === 'string')
    {
      return [objToMap]
    }

    return Object.keys(objToMap).map<string>(v => {
      return Object.getOwnPropertyDescriptor(objToMap, v).value
    })
  }
  catch(e){
    console.log("Failed to get errors: " + e)
    return []
  }
}
export function toggleVisuallyHidden(element:HTMLElement) {
  const className = 'visually-hidden';
  if (element.classList.contains(className)) {
    element.classList.remove(className);
  } else {
    element.classList.add(className);
  }
}

// export an array of key value pairs representing strategies
export function GetStrategies(): { key: string, value: string }[] {
    return [
        { key: "10x", value: "10x" },
        { key: "discretionary", value: "Discretionary" },
        { key: "channelbottom", value: "Channel Bottom" },
        { key: "leadingindustry", value: "Leading Industry" },
        { key: "longterm", value: "Long Term" },
        { key: "longterminterest", value: "Long Term Interest" },
        { key: "newhigh", value: "New High" },
        { key: "newhighpullback", value: "New High Pullback" },
        { key: "postearnings", value: "Post Earnings" },
        { key: "postearningsnewhigh", value: "Post Earnings New High" },
        { key: "recovery", value: "Recovery" },
        { key: "resistancebreakthrough", value: "Resistance Breakthrough" },
        { key: "weeklypullbreak", value: "Weekly Pull/Break" },
        { key: "weeklypullbreakdelayed", value: "Weekly Pull/Break Delayed" },
    ]
}

export function isLongTermStrategy(strategy:string) {
    return strategy === "longterm" || strategy === "longterminterest"
}
