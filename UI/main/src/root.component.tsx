///<reference path="euis.d.ts" />

import { Component } from "react";
import { Cs2FormLine } from "./components/_common/Cs2FormLine";
import Cs2Select from "./components/_common/cs2-select";
import { Input, SimpleInput } from "#components/_common/input";


export type Entity = {
  __Type: 'Unity.Entities.Entity, Unity.Entities, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null';
  Index: number;
  Version: number;
};

type State = {
  currentEntity: Entity,
  fontsLoaded: { name: string }[],
  selectedFont?: { name: string },
  textToRender?: string
}

export default class Root extends Component<{}, State> {

  constructor(props) {
    super(props)
  }

  componentDidMount(): void {
    engine.on("k45::we.test.enableTestTool->", this.onSelectEntity)
    engine.on("k45::we.test.fontsChanged->", this.onFontsChanged)
    engine.call("k45::we.test.listFonts").then((x) => this.setState({ fontsLoaded: (x as string[]).map(x => { return { name: x } }) }));
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
    this.setState({ fontsLoaded: (e as string[]).map(x => { return { name: x } }) });
  }

  render() {
    return <ErrorBoundary>
      <h1>ON!</h1>
      <button onClick={() => location.reload()} className="normalBtn">REFRESH PAGE</button>
      <button onClick={() => engine.call("k45::we.test.enableTestTool")} className="normalBtn">Enable tool! !@!@</button>
      <pre>{JSON.stringify(this.state?.currentEntity, null, 2)}</pre>
      <button onClick={() => engine.call("k45::we.test.reloadFonts")} className="positiveBtn">Reload Fonts</button>
      <Cs2FormLine title="Select Font">
        <Cs2Select
          options={this.state?.fontsLoaded}
          getOptionLabel={(x) => x.name}
          getOptionValue={(x) => x.name}
          onChange={(x) => this.setState({ selectedFont: x })}
          value={this.state?.selectedFont}
        />
      </Cs2FormLine>
      <Cs2FormLine title="Text to render">
        <SimpleInput onValueChanged={(y) => {
          this.setState({ textToRender: y });
          return y;
        }} maxLength={512} getValue={() => this.state?.textToRender} />
      </Cs2FormLine>
      <button className="negativeBtn" onClick={() => engine.call("k45::we.test.requestTextMesh", this.state?.textToRender, this.state?.selectedFont?.name).then(console.log)}>Generate text...</button>

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
