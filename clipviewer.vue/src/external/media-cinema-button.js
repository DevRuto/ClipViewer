import { MediaChromeButton } from 'media-chrome'

const theatreModeIcon = `<svg aria-hidden="true" viewBox="0 0 24 24">
  <path d="M3 5v14h18V5H3zm16 12H5V7h14v10z"/>
  <path d="M7 9h10v6H7z"/>
</svg>`

const cinemaModeIcon = `<svg aria-hidden="true" viewBox="0 0 24 24">
  <path d="M2 4v16h20V4H2zm18 14H4V6h16v12z"/>
  <path d="M6 8h12v8H6z"/>
</svg>`

let initialCinemaMode = localStorage.getItem('cinema-mode') === 'true'
function tooltipContentHTML(isCinemaMode) {
  return isCinemaMode ? 'Exit Cinema Mode' : 'Enter Cinema Mode'
}

class MediaCinemaButton extends MediaChromeButton {
  constructor() {
    super()
    this.isCinemaMode = localStorage.getItem('cinema-mode') === 'true'
  }

  connectedCallback() {
    super.connectedCallback()
    this.requestUpdate()
  }

  static get properties() {
    return {
      ...super.properties,
      isCinemaMode: { type: Boolean },
    }
  }

  static getSlotTemplateHTML() {
    return `
    <style>
    slot[name=icon] slot:not(name=${!this.isCinemaMode ? 'enabled' : 'disabled'}) {
      display: none !important;
    }
    slot[name=icon] slot:not(name=${this.isCinemaMode ? 'enabled' : 'disabled'}) {
      display: none !important;
    }
    </style>
    <slot name="icon">
     <slot name="enabled">${cinemaModeIcon}</slot>
     <slot name="disabled">${theatreModeIcon}</slot>
    </slot>
    `
  }

  static getTooltipContentHTML() {
    return tooltipContentHTML(initialCinemaMode)
  }

  handleClick() {
    this.isCinemaMode = !this.isCinemaMode
    localStorage.setItem('cinema-mode', this.isCinemaMode.toString())
    this.requestUpdate()
  }

  requestUpdate() {
    const slot = this.shadowRoot.querySelector('slot[name=icon]')
    if (slot) {
      slot.innerHTML = this.isCinemaMode ? cinemaModeIcon : theatreModeIcon
    }
    const tooltipSlot = this.shadowRoot.querySelector('slot[name=tooltip-content]')
    if (tooltipSlot) {
      tooltipSlot.innerHTML = tooltipContentHTML(this.isCinemaMode)
    }
    const eventName = this.isCinemaMode ? 'cinema-mode-enabled' : 'cinema-mode-disabled'
    this.dispatchEvent(
      new CustomEvent(eventName, {
        bubbles: true,
        composed: true,
        detail: { isCinemaMode: this.isCinemaMode },
      }),
    )
  }
}

if (!globalThis.customElements.get('media-cinema-button')) {
  globalThis.customElements.define('media-cinema-button', MediaCinemaButton)
}

export default MediaCinemaButton
