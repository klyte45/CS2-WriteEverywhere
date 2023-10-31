///<reference path="euis.d.ts" />

import { Component } from "react";


export type Entity = {
  __Type: 'Unity.Entities.Entity, Unity.Entities, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null';
  Index: number;
  Version: number;
};

type State = {
  currentEntity: Entity,
  fontsLoaded: string[]
}

export default class Root extends Component<{}, State> {

  constructor(props) {
    super(props)
  }

  componentDidMount(): void {
    engine.on("k45::we.test.enableTestTool->", this.onSelectEntity)
    engine.on("k45::we.test.fontsChanged->", this.onFontsChanged)
  }
  componentWillUnmount(): void {
    engine.off("k45::we.test.enableTestTool->")
  }

  private onSelectEntity = (e: Entity) => {
    console.log(e)
    this.setState({ currentEntity: e });
  }
  private onFontsChanged = (e: string[]) => {
    console.log(e)
    this.setState({ fontsLoaded: e });
  }

  render() {
    return <ErrorBoundary>
      <h1>ON!</h1>
      <button onClick={() => location.reload()}>REFRESH PAGE</button>
      <button onClick={() => engine.call("k45::we.test.enableTestTool")}>Enable tool! !@!@</button>
      <pre>{JSON.stringify(this.state?.currentEntity, null, 2)}</pre>
      <button onClick={() => engine.call("k45::we.test.reloadFonts")}>Reload Fonts</button>
      <pre>Fonts loaded: {(this.state?.fontsLoaded ?? [])}</pre>
    </ErrorBoundary>;
  }
}

class ErrorBoundary extends Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false };
  }

  componentDidCatch(error, info) {
    // Display fallback UI
    this.setState({ hasError: true });
    // You can also log the error to an error reporting service
    console.log(error, info);
  }

  render() {
    if ((this.state as any)?.hasError) {
      // You can render any custom fallback UI
      return <h1>Something went wrong.</h1>;
    }
    return this.props.children;
  }
}
