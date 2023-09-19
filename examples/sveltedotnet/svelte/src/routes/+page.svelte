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
        endpoint: "https://localhost:7134/api/upload",
        uploadDataDuringCreation: false,
        allowedMetaFields: ["name", "type"]
      });
  });
</script>

<h1>Welcome to SvelteKit</h1>
<p>Visit <a href="https://kit.svelte.dev">kit.svelte.dev</a> to read the documentation</p>

<div id="dashboard"></div>