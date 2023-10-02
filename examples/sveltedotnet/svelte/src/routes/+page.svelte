<script lang="ts">
  import Uppy from '@uppy/core';
  import Dashboard from '@uppy/dashboard';
  import Tus from "@uppy/tus";

  // Don't forget the CSS: core and UI components + plugins you are using
  import '@uppy/core/dist/style.css';
  import '@uppy/dashboard/dist/style.css';
  import '@uppy/dashboard/dist/style.min.css';
  import { onMount } from 'svelte';

  onMount(() => {
    const uppy = new Uppy({
      id: "lalal",
      allowMultipleUploadBatches: true,
      restrictions: {
        maxNumberOfFiles: 1,
        maxFileSize: 5_000_000_000,
        allowedFileTypes: [
          ".pdf",
          ".docx",
          ".pptx",
          ".html",
          ".iso"]
        }
      });

    uppy
      .use(Dashboard,{inline:true, target:"#dashboard"})
      .use(Tus, {
        endpoint,
        uploadDataDuringCreation: false,
        allowedMetaFields: ["name", "type"],
        parallelUploads: 2
      });

    updateLocalStorage();
  });

  let defaultItems: any[] = [];
  $: items = defaultItems;

  const updateLocalStorage = () => {
    items = [];
    for (let [key, value] of Object.entries(localStorage)) {
      let obj = JSON.parse(value);
      items.push(obj);
    }
  }

  $: endpoint = "https://localhost:7134/api/upload";
</script>

<h1>Solidtus using @uppy/tus</h1>

{#each items as item}
<div>
  <p>Size: {item.size}</p>
  <p>Metadata: {JSON.stringify(item.metadata)}</p>
  <p>Creation Time: {item.creationTime}</p>
  <p>Upload Url: {item.uploadUrl}</p>
</div>
{/each}

<div>
  <input type="text" bind:value={endpoint}>
</div>


<button on:click={updateLocalStorage}>
  Update local storage
</button>

<button on:click={() => { localStorage.clear(); items = []; }}>
  Clear the LocalStorage
</button>

<div id="dashboard"></div>