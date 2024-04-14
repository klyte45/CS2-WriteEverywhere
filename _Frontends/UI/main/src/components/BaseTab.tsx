import { Component } from "react";
import { Entity } from "../root.component";
import { Cs2FormLine, Cs2Select, SimpleInput } from "@klyte45/euis-components";


type State = {
  currentEntity: Entity,
  fontsLoaded: { name: string }[],
  selectedFont?: { name: string },
  textToRender?: string,
  currentOverlay: number,
  shaderList?: { name: string }[]
}


export class BaseTab extends Component<{}, State> {

  constructor(props) {
    super(props);
    this.state ??= { currentOverlay: 0 } as any;
  }

  componentDidMount(): void {
    engine.on("k45::we.test.enableTestTool->", this.onSelectEntity);
    engine.on("k45::we.test.fontsChanged->", this.onFontsChanged);
    engine.call("k45::we.test.listFonts").then((x) => this.setState({ fontsLoaded: (x as string[]).map(x => { return { name: x }; }) }));
    engine.call("k45::we.test.getOverlay").then((x) => this.setState({ currentOverlay: x }));
  }
  componentWillUnmount(): void {
    engine.off("k45::we.test.enableTestTool->");
  }

  private onSelectEntity = (e: Entity) => {
    console.log(e);
    this.setState({ currentEntity: e });
  };
  private onFontsChanged = (e: string[]) => {
    console.log(e);
    this.setState({ fontsLoaded: (e as string[]).map(x => { return { name: x }; }) });
  };

  render() {
    return <>
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
          value={this.state?.selectedFont} />
      </Cs2FormLine>
      <Cs2FormLine title="Text to render">
        <SimpleInput onValueChanged={(y) => {
          this.setState({ textToRender: y });
          return y;
        }} maxLength={512} getValue={() => this.state?.textToRender} />
      </Cs2FormLine>
      <Cs2FormLine title="Overlay">
        <SimpleInput onValueChanged={(y) => {
          engine.call("k45::we.test.setOverlay", parseInt(y, 16) || 0).then(x => {
            this.setState({ currentOverlay: x })
          });
          return (parseInt(y, 16).toString(16) || 0) + "";
        }} maxLength={8} getValue={() => this.state?.currentOverlay.toString(16)} />
      </Cs2FormLine>
      <button className="negativeBtn" onClick={() => engine.call("k45::we.test.requestTextMesh", this.state?.textToRender, this.state?.selectedFont?.name).then(console.log)}>Generate text...</button>
    </>;
  }
}
