/****** Script for SelectTopNRows command from SSMS  ******/
SELECT
	rp.[PerformanceId] as 'pid'
      ,rp.[ArtistId] as 'aid'
	  ,a.[Name] as 'artist'
	  ,r.Id as [rid]
	  ,r.[Name] as 'raga'
	  ,t.MovementNumber as ' '
	  ,t.Title
  FROM [music].[RagaPerformances] rp
  inner join music.Artists a on rp.ArtistId = a.Id
  inner join music.Ragas r on rp.RagaId = r.Id
  inner join music.Tracks t on rp.PerformanceId = t.PerformanceId
  order by rp.PerformanceId, t.MovementNumber