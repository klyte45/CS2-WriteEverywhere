interface ArbitraryCallback {
    (...args: any[]);
}
interface EventHandle {
    /**
    * Detach this handler from the event
    */
    clear(): void;
}

interface Deferred<T> extends Promise<T> {
    /**
    * Resolve the promise with the specified value. All success handlers will be called with value
    * @param value the success value of the promise
    */
    resolve(value: T): void;

    /**
    * Reject the promise with the specified value. All failure handlers will be called with value
    * @param value the failure value of the promise
    */
    reject(value: T): void;
}

interface CustomAttributeBinding {
    update(element: HTMLElement, value: any)
    init(element: HTMLElement, value: any)
}

interface Engine {
    /**
    * Register handler for an event
    * @param name name of the event
    * @param callback function to be executed when the event has been triggered
    * @param context *this* context for the function, by default the engine object
    * @return handle for unsubscribing this callback to the event
    */
    on(name: string, callback: ArbitraryCallback, context?: any): EventHandle;

    /**
    * Remove handler for an event
    * @param name name of the event, by default removes all events
    * @param callback the function to be removed, by default removes all callbacks for a given event
    * @param context *this* context for the function, by default all removes all callbacks, regardless of context
    */
    off(name: string, callback?: ArbitraryCallback, context?: any): void;

    /**
    * @function engine.trigger
    * Trigger an event
    * This function will trigger any C++ handler registered for this event with `Coherent::UI::View::RegisterForEvent`
    * @param  name name of the event
    * @param args any extra arguments to be passed to the event handlers
    */
    trigger(name: string, ...args: any[]): void;

    /**
    * Call asynchronously a C++ handler and retrieve the result
    * The C++ handler must have been registered with `Coherent::UI::View::BindCall`
    * @param name name of the C++ handler to be called
    * @param args any extra parameters to be passed to the C++ handler
    * @return promise for the result of the C++ function
    */
    call(name: string, ...args: any[]): Promise<any>;

    isAttached: boolean;
    hasAttachedUpdateListner: boolean
    readonly whenReady: Promise<any>
    createJSModel(name: string, mock?: any): void
    updateWholeModel<M>(model: M): void
    unregisterModel<M>(model: M): void
    createObservableModel(observableName: string): void
    addSynchronizationDependency<M, D>(model: M, dependentModel: D): void
    removeSynchronizationDependency<M>(model: M): void
    synchronizeModels(): void;
    registerBindingAttribute(name: string, handler: CustomAttributeBinding): void;
    unregisterModel(name: string): void
    reloadLocalization(): void;
    translate(id: string): string;
}

declare var engine: Engine;
interface EuisMainModel {
    currentTime: string,
    currentDate: string,
    monitorIdentifierText: string,
    monitorNumber: number
}

declare var __euis_main: EuisMainModel