export function GetErrors(err:any): string[] {
  try{
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
  
  export function HideIfHidden(value, hidden) {
    return hidden ? 0 : value;
  }
  
  export function toggleVisuallyHidden(element:HTMLElement) {
    const className = 'visually-hidden';
    if (element.classList.contains(className)) {
      element.classList.remove(className);
    } else {
      element.classList.add(className);
    }
  }