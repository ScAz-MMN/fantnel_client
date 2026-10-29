<template>
  <div class="tooltip-container">
    <slot></slot>
    <span class="tooltip-text" :style="{ width: width, marginLeft: `-${parseInt(width) / 2}px` }">
      <slot name="tooltip"></slot>
    </span>
  </div>
</template>

<script setup>
defineProps({
  width: {
    type: String,
    default: '200px'
  }
})
</script>

<style scoped>
.tooltip-container {
  position: relative;
  display: inline-block;
  cursor: help;
}

.tooltip-text {
  visibility: hidden;
  background-color: var(--surface-2);
  color: var(--text-color);
  border: 1px solid var(--border-color);
  text-align: center;
  border-radius: var(--radius-sm);
  padding: 9px 12px;
  font-size: 0.8rem;
  white-space: nowrap;
  position: absolute;
  z-index: 100;
  bottom: 125%;
  left: 50%;
  opacity: 0;
  box-shadow: 0 6px 18px var(--shadow-strong);
  transition: opacity 0.2s ease;
  pointer-events: none;
}

.tooltip-container:hover .tooltip-text {
  visibility: visible;
  opacity: 1;
}

.tooltip-text::after {
  content: "";
  position: absolute;
  top: 100%;
  left: 50%;
  margin-left: -5px;
  border-width: 5px;
  border-style: solid;
  border-color: var(--surface-2) transparent transparent transparent;
}
</style>